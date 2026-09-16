using System.IO;
using System.Text;
using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using ContractManagement.Application.AI.Services;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Features.Attachments;
using ContractManagement.Domain;
using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contracts.Enums;
using ContractManagement.Infrastructure.Persistence;
using ContractManagement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using DomainContract = ContractManagement.Domain.Contracts.Entities.Contract;

namespace ContractManagement.UnitTests.AI;

/// <summary>
/// Focused integration tests for AI attachment → analysis path.
/// Verifies the blocker fix: uploaded attachment becomes the file reference consumed by AI.
/// Uses CURRENT Contract entity (dbo.CONTRACTS), not LEGACY_CONTRACTS.
/// </summary>
public class AIAttachmentIntegrationTests : IDisposable
{
    private readonly ContractManagementDbContext _context;
    private readonly Mock<IStorageProvider> _storageProviderMock;
    private readonly Mock<IDocumentTextExtractor> _extractorMock;
    private readonly Mock<IAIContractAssistantService> _aiMock;

    public AIAttachmentIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ContractManagementDbContext(options);
        _storageProviderMock = new Mock<IStorageProvider>(MockBehavior.Strict);
        _extractorMock = new Mock<IDocumentTextExtractor>(MockBehavior.Strict);
        _aiMock = new Mock<IAIContractAssistantService>(MockBehavior.Strict);
    }

    public void Dispose() => _context.Dispose();

    private static DomainContract CreateContract(Guid id, string? fileUrl = null)
    {
        return new DomainContract
        {
            Id = id,
            ContractNumber = "HD-" + id.ToString()[..8],
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Test Contract",
            Value = 1000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.Draft,
            FileUrl = fileUrl,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 1 }
        };
    }

    private void SetupAiSuccess(string fileRef, byte[] fileBytes, string extractedText)
    {
        _extractorMock.Setup(e => e.ExtractTextAsync(fileRef, fileBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);
        _aiMock.Setup(a => a.ExtractContractInfoAsync(
                It.Is<ExtractContractRequest>(r => r.ContractContent == extractedText),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedContractInfoDto { Value = 5000m, ExpiryDate = new DateTime(2027, 1, 1) });
        _aiMock.Setup(a => a.SummarizeContractAsync(
                It.Is<SummarizeContractRequest>(r => r.ContractContent == extractedText),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContractSummaryDto { ContractId = Guid.Empty, Summary = "summary", KeyPoints = new List<string>() });
    }

    // 1. Upload syncs Contract.FileUrl — first version (verifies CURRENT CONTRACTS mapping)
    [Fact]
    public async Task UploadAttachment_ShouldSyncContractFileUrl_OnFirstVersion()
    {
        var contractId = Guid.NewGuid();
        var contract = CreateContract(contractId);
        ((IAttachmentDbContext)_context).Contracts.Add(contract);
        await _context.SaveChangesAsync();

        var expectedUrl = $"storage/contracts/{contractId}/v1_hop_dong.pdf";
        var storageMock = new Mock<IStorageService>();
        storageMock.Setup(s => s.SaveFileAsync(contractId, 1, "hop_dong.pdf", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);
        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var handler = new UploadAttachmentCommandHandler(_context, storageMock.Object, userMock.Object);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("pdf content"));
        var result = await handler.Handle(new UploadAttachmentCommand
        {
            ContractId = contractId,
            FileName = "hop_dong.pdf",
            FileStream = stream
        }, CancellationToken.None);

        result.FileUrl.Should().Be(expectedUrl);
        var reloaded = await ((IAttachmentDbContext)_context).Contracts.FirstAsync(c => c.Id == contractId);
        reloaded.FileUrl.Should().Be(expectedUrl);
        // Also verify via IContractDbContext / direct DbSet (dbo.CONTRACTS)
        var viaCurrent = await _context.Contracts.FirstAsync(c => c.Id == contractId);
        viaCurrent.FileUrl.Should().Be(expectedUrl);
    }

    // 2. Upload syncs Contract.FileUrl — incremented version resolves to latest
    [Fact]
    public async Task UploadAttachment_ShouldSyncContractFileUrl_OnSubsequentVersion()
    {
        var contractId = Guid.NewGuid();
        var contract = CreateContract(contractId);
        ((IAttachmentDbContext)_context).Contracts.Add(contract);
        _context.Attachments.Add(new Attachment(Guid.NewGuid(), contractId, "v1.pdf", 1, $"storage/contracts/{contractId}/v1_v1.pdf", Guid.NewGuid(), DateTime.UtcNow.AddHours(-2)));
        _context.Attachments.Add(new Attachment(Guid.NewGuid(), contractId, "v2.pdf", 2, $"storage/contracts/{contractId}/v2_v2.pdf", Guid.NewGuid(), DateTime.UtcNow.AddHours(-1)));
        await _context.SaveChangesAsync();

        var expectedUrl = $"storage/contracts/{contractId}/v3_v3.pdf";
        var storageMock = new Mock<IStorageService>();
        storageMock.Setup(s => s.SaveFileAsync(contractId, 3, "v3.pdf", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);
        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var handler = new UploadAttachmentCommandHandler(_context, storageMock.Object, userMock.Object);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("v3 content"));
        var result = await handler.Handle(new UploadAttachmentCommand
        {
            ContractId = contractId,
            FileName = "v3.pdf",
            FileStream = stream
        }, CancellationToken.None);

        result.Version.Should().Be(3);
        var reloaded = await ((IAttachmentDbContext)_context).Contracts.FirstAsync(c => c.Id == contractId);
        reloaded.FileUrl.Should().Be(expectedUrl);
    }

    // 3. AI fallback to latest Attachment when Contract.FileUrl is null — uses IStorageService layout
    [Fact]
    public async Task AnalyzeContractAsync_WhenContractFileUrlNull_ShouldFallbackToLatestAttachmentViaStorageService()
    {
        var contractId = Guid.NewGuid();
        _context.Contracts.Add(CreateContract(contractId, null));
        var olderUrl = $"storage/contracts/{contractId}/v1_old.pdf";
        var latestUrl = $"storage/contracts/{contractId}/v2_latest.pdf";
        _context.Attachments.Add(new Attachment(Guid.NewGuid(), contractId, "old.pdf", 1, olderUrl, Guid.NewGuid(), DateTime.UtcNow.AddHours(-2)));
        _context.Attachments.Add(new Attachment(Guid.NewGuid(), contractId, "latest.pdf", 2, latestUrl, Guid.NewGuid(), DateTime.UtcNow.AddHours(-1)));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1, 2, 3 };
        var extractedText = "contract text from latest";
        SetupAiSuccess(latestUrl, fileBytes, extractedText);

        var storageServiceMock = new Mock<IStorageService>(MockBehavior.Strict);
        storageServiceMock.Setup(s => s.GetFileAsync(latestUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        // Ensure provider is NOT called when service succeeds
        var sut = new AIAnalysisJobService(_context, _context, _storageProviderMock.Object, _extractorMock.Object, _aiMock.Object, storageServiceMock.Object, _context);

        await sut.AnalyzeContractAsync(contractId);

        storageServiceMock.Verify(s => s.GetFileAsync(latestUrl, It.IsAny<CancellationToken>()), Times.Once);
        _storageProviderMock.Verify(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        var persisted = await _context.AiAnalysisResults.FirstOrDefaultAsync(r => r.ContractId == contractId);
        persisted.Should().NotBeNull();
    }

    // 4. AI prefers Contract.FileUrl via IStorageService — happy path after upload sync
    [Fact]
    public async Task AnalyzeContractAsync_WhenContractFileUrlPresent_ShouldUseStorageService()
    {
        var contractId = Guid.NewGuid();
        var fileUrl = $"storage/contracts/{contractId}/v1_doc.pdf";
        _context.Contracts.Add(CreateContract(contractId, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 9, 8, 7 };
        var text = "pdf doc text";
        SetupAiSuccess(fileUrl, fileBytes, text);

        var storageServiceMock = new Mock<IStorageService>(MockBehavior.Strict);
        storageServiceMock.Setup(s => s.GetFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        var sut = new AIAnalysisJobService(_context, _context, _storageProviderMock.Object, _extractorMock.Object, _aiMock.Object, storageServiceMock.Object, _context);

        await sut.AnalyzeContractAsync(contractId);

        storageServiceMock.Verify(s => s.GetFileAsync(fileUrl, It.IsAny<CancellationToken>()), Times.Once);
        var persisted = await _context.AiAnalysisResults.FirstAsync(r => r.ContractId == contractId);
        persisted.ContractId.Should().Be(contractId);
    }

    // 5. AI falls back to IStorageProvider when IStorageService file not found (backwards compat)
    [Fact]
    public async Task AnalyzeContractAsync_WhenStorageServiceThrowsNotFound_ShouldFallbackToProvider()
    {
        var contractId = Guid.NewGuid();
        var fileUrl = "contract.pdf";
        _context.Contracts.Add(CreateContract(contractId, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 5, 6, 7 };
        var text = "fallback text";
        SetupAiSuccess(fileUrl, fileBytes, text);

        var storageServiceMock = new Mock<IStorageService>(MockBehavior.Strict);
        storageServiceMock.Setup(s => s.GetFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("not found"));
        _storageProviderMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileBytes);

        var sut = new AIAnalysisJobService(_context, _context, _storageProviderMock.Object, _extractorMock.Object, _aiMock.Object, storageServiceMock.Object, _context);

        await sut.AnalyzeContractAsync(contractId);

        _storageProviderMock.Verify(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()), Times.Once);
        (await _context.AiAnalysisResults.CountAsync(r => r.ContractId == contractId)).Should().Be(1);
    }

    // 6. End-to-end: Upload then Analyze uses same fileRef without manual Contract.FileUrl patching
    [Fact]
    public async Task UploadThenAnalyze_ShouldUseSameStorageLayout_EndToEnd()
    {
        var contractId = Guid.NewGuid();
        ((IAttachmentDbContext)_context).Contracts.Add(CreateContract(contractId));
        await _context.SaveChangesAsync();

        // Simulate upload
        var fileUrl = $"storage/contracts/{contractId}/v1_contract.pdf";
        var storageMock = new Mock<IStorageService>();
        storageMock.Setup(s => s.SaveFileAsync(contractId, 1, "contract.pdf", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileUrl);
        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        var handler = new UploadAttachmentCommandHandler(_context, storageMock.Object, userMock.Object);
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("original pdf bytes"));
        await handler.Handle(new UploadAttachmentCommand { ContractId = contractId, FileName = "contract.pdf", FileStream = uploadStream }, CancellationToken.None);

        // Verify sync via both contexts (dbo.CONTRACTS)
        var afterUpload = await _context.Contracts.FirstAsync(c => c.Id == contractId);
        afterUpload.FileUrl.Should().Be(fileUrl);
        var afterUploadViaAttachment = await ((IAttachmentDbContext)_context).Contracts.FirstAsync(c => c.Id == contractId);
        afterUploadViaAttachment.FileUrl.Should().Be(fileUrl);

        // Now analyze — wire a real temp storage service backed by filesystem for this contract's file
        var tempDir = Path.Combine(Path.GetTempPath(), "ai_attach_int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var realStorage = new LocalStorageService(tempDir);
            // Write the file to real storage so GetFileAsync succeeds
            using var writeStream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF fake"));
            await realStorage.SaveFileAsync(contractId, 1, "contract.pdf", writeStream);

            // Need actual bytes for extractor — mock extractor/ AI to avoid real PDF parsing
            var fileBytes = new byte[] { 1, 2, 3 };
            // Override realStorage GetFileAsync by mocking? Instead use Mock that returns fileBytes for fileUrl.
            var storageServiceMock = new Mock<IStorageService>();
            storageServiceMock.Setup(s => s.GetFileAsync(fileUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(fileBytes));
            SetupAiSuccess(fileUrl, fileBytes, "extracted via attachment");

            var sut = new AIAnalysisJobService(_context, _context, _storageProviderMock.Object, _extractorMock.Object, _aiMock.Object, storageServiceMock.Object, _context);
            await sut.AnalyzeContractAsync(contractId);
            (await _context.AiAnalysisResults.CountAsync(r => r.ContractId == contractId)).Should().Be(1);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
