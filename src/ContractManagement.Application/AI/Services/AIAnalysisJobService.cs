using System.IO;
using System.Linq;
using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Domain.AI.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.AI.Services;

/// <summary>
/// Application job service that orchestrates AI contract analysis.
/// Intended to be invoked by Hangfire (or any scheduler) in a future step;
/// this class itself has no Hangfire dependency.
/// </summary>
/// <remarks>
/// Future flow: HTTP Controller → Hangfire enqueue → <see cref="IAIAnalysisJobService"/>
/// → Contract → <see cref="IStorageProvider"/> → <see cref="IDocumentTextExtractor"/>
/// → <see cref="IAIContractAssistantService"/> → <see cref="AiAnalysisResult"/> → AI_ANALYSIS_RESULTS.
///
/// Idempotency: simplest strategy — creates a new row per execution.
/// AI_ANALYSIS_RESULTS has a non-unique index on ContractId, so multiple rows per contract are allowed.
///
/// Schema limitation: <see cref="ExtractedContractInfoDto"/> contains ContractType and SignedDate,
/// but AI_ANALYSIS_RESULTS has no columns for them. Those fields are extracted but not persisted.
/// If SRS requires persistence of those fields, the schema must be extended in a separate migration step.
/// </remarks>
public class AIAnalysisJobService : IAIAnalysisJobService
{
    private readonly IContractManagementDbContext _contractContext;
    private readonly IAiDbContext _aiContext;
    private readonly IStorageProvider _storageProvider;
    private readonly IStorageService? _storageService;
    private readonly IAttachmentDbContext? _attachmentContext;
    private readonly IDocumentTextExtractor _documentTextExtractor;
    private readonly IAIContractAssistantService _aiAssistant;

    public AIAnalysisJobService(
        IContractManagementDbContext contractContext,
        IAiDbContext aiContext,
        IStorageProvider storageProvider,
        IDocumentTextExtractor documentTextExtractor,
        IAIContractAssistantService aiAssistant,
        IStorageService? storageService = null,
        IAttachmentDbContext? attachmentContext = null)
    {
        _contractContext = contractContext ?? throw new ArgumentNullException(nameof(contractContext));
        _aiContext = aiContext ?? throw new ArgumentNullException(nameof(aiContext));
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _documentTextExtractor = documentTextExtractor ?? throw new ArgumentNullException(nameof(documentTextExtractor));
        _aiAssistant = aiAssistant ?? throw new ArgumentNullException(nameof(aiAssistant));
        _storageService = storageService;
        _attachmentContext = attachmentContext;
    }

    public async Task AnalyzeContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        if (contractId == Guid.Empty)
            throw new ArgumentException("ContractId must not be empty.", nameof(contractId));

        cancellationToken.ThrowIfCancellationRequested();

        // 1. Load contract and validate existence.
        var contract = await _contractContext.Contracts
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            throw new InvalidOperationException($"Contract '{contractId}' was not found.");

        cancellationToken.ThrowIfCancellationRequested();

        // 2. Resolve file reference — prefer Contract.FileUrl (kept in sync on upload),
        // fallback to latest Attachment for legacy contracts created before the sync.
        var fileRef = contract.FileUrl;
        if (string.IsNullOrWhiteSpace(fileRef) && _attachmentContext != null)
        {
            fileRef = await _attachmentContext.Attachments
                .Where(a => a.ContractId == contractId)
                .OrderByDescending(a => a.Version)
                .Select(a => a.FileUrl)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(fileRef))
            throw new InvalidOperationException($"Contract '{contractId}' does not have an associated file.");

        // 3. Download file bytes — prefer IStorageService (the writer of attachment files)
        // to guarantee the same path/layout (storage/contracts/{id}/v{ver}_{name}).
        // Fallback to IStorageProvider for backwards compat when FileUrl was set via Contract create/update.
        byte[] fileBytes;
        if (_storageService != null)
        {
            try
            {
                await using var stream = await _storageService.GetFileAsync(fileRef, cancellationToken);
                await using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }
            catch (FileNotFoundException)
            {
                fileBytes = await _storageProvider.DownloadFileAsync(fileRef, cancellationToken);
            }
        }
        else
        {
            fileBytes = await _storageProvider.DownloadFileAsync(fileRef, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 4. Extract text — determines format from file name/extension, rejects unsupported/empty.
        // NotSupportedException for unsupported format, InvalidOperationException for empty text propagate.
        var extractedText = await _documentTextExtractor.ExtractTextAsync(fileRef, fileBytes, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // 5. Call AI services with extracted text.
        var extractedInfo = await _aiAssistant.ExtractContractInfoAsync(
            new ExtractContractRequest { ContractId = contractId, ContractContent = extractedText },
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var summary = await _aiAssistant.SummarizeContractAsync(
            new SummarizeContractRequest { ContractId = contractId, ContractContent = extractedText },
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // 6. Create result using only existing schema fields.
        // ExtractedContractInfoDto.ContractType / SignedDate are not persisted (no columns).
        var result = new AiAnalysisResult(
            contractId: contractId,
            summary: summary.Summary,
            extractedValue: extractedInfo.Value,
            extractedExpiryDate: extractedInfo.ExpiryDate,
            riskFlags: null);

        _aiContext.AiAnalysisResults.Add(result);

        // 7. Persist — persistence failures propagate.
        await _aiContext.SaveChangesAsync(cancellationToken);
    }
}
