using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contract.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContractManagement.Api.Controllers.AI;

/// <summary>
/// AI Contract Assistant API endpoints for contract analysis operations.
/// Provides endpoints for extracting contract information, summarization, and risk analysis,
/// plus Hangfire-backed contract-level analysis enqueue and retrieval.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("AI Contract Assistant")]
[Authorize]
public class AIContractAssistantController : ControllerBase
{
    private readonly IAIContractAssistantService _aiService;
    private readonly IContractManagementDbContext _contractContext;
    private readonly IAiDbContext _aiContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AIContractAssistantController> _logger;

    [ActivatorUtilitiesConstructor]
    public AIContractAssistantController(
        IAIContractAssistantService aiService,
        IContractManagementDbContext contractContext,
        IAiDbContext aiContext,
        ICurrentUserService currentUserService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<AIContractAssistantController> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _contractContext = contractContext ?? throw new ArgumentNullException(nameof(contractContext));
        _aiContext = aiContext ?? throw new ArgumentNullException(nameof(aiContext));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Backwards-compatible constructor for tests that only exercise the legacy AI endpoints
    /// without Hangfire or DB. Supplies no-op fakes for the new dependencies.
    /// Prefer the full constructor in production and new tests.
    /// </summary>
    public AIContractAssistantController(
        IAIContractAssistantService aiService,
        ILogger<AIContractAssistantController> logger)
        : this(
            aiService,
            new FakeContractContext(),
            new FakeAiContext(),
            new FakeCurrentUserService(),
            new FakeBackgroundJobClient(),
            logger)
    {
    }

    // ------------------------------------------------------------------
    // Legacy AI endpoints (synchronous, content supplied by caller)
    // ------------------------------------------------------------------

    /// <summary>
    /// Extract key information from a contract using AI analysis.
    /// </summary>
    [HttpPost("extract")]
    public async Task<ActionResult<ExtractedContractInfoDto>> ExtractContractInfo(
        [FromBody] ExtractContractRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request?.ContractId == Guid.Empty)
                return BadRequest(new { error = "ContractId must be a valid GUID" });

            _logger.LogInformation("Extracting contract information for ContractId: {ContractId}", request!.ContractId);
            var result = await _aiService.ExtractContractInfoAsync(request, cancellationToken);
            _logger.LogInformation("Successfully extracted contract information for ContractId: {ContractId}", request.ContractId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in extract request for ContractId: {ContractId}", request?.ContractId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation failed during extract for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Extract operation cancelled for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status408RequestTimeout, new { error = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during extract for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Generate a concise summary of a contract using AI analysis.
    /// </summary>
    [HttpPost("summarize")]
    public async Task<ActionResult<ContractSummaryDto>> SummarizeContract(
        [FromBody] SummarizeContractRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request?.ContractId == Guid.Empty)
                return BadRequest(new { error = "ContractId must be a valid GUID" });

            if (string.IsNullOrWhiteSpace(request?.ContractContent))
                return BadRequest(new { error = "ContractContent cannot be empty" });

            _logger.LogInformation("Summarizing contract for ContractId: {ContractId}", request.ContractId);
            var result = await _aiService.SummarizeContractAsync(request, cancellationToken);
            _logger.LogInformation("Successfully summarized contract for ContractId: {ContractId}", request.ContractId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in summarize request for ContractId: {ContractId}", request?.ContractId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation failed during summarize for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Summarize operation cancelled for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status408RequestTimeout, new { error = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during summarize for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Analyze a contract for potential risks and issues using AI.
    /// </summary>
    [HttpPost("analyze-risk")]
    public async Task<ActionResult<ContractRiskAnalysisDto>> AnalyzeContractRisk(
        [FromBody] AnalyzeContractRiskRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request?.ContractId == Guid.Empty)
                return BadRequest(new { error = "ContractId must be a valid GUID" });

            if (string.IsNullOrWhiteSpace(request?.ContractContent))
                return BadRequest(new { error = "ContractContent cannot be empty" });

            _logger.LogInformation("Analyzing contract risks for ContractId: {ContractId}", request.ContractId);
            var result = await _aiService.AnalyzeContractRiskAsync(request, cancellationToken);
            _logger.LogInformation("Successfully analyzed contract risks for ContractId: {ContractId}", request.ContractId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in risk analysis request for ContractId: {ContractId}", request?.ContractId);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operation failed during risk analysis for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Risk analysis operation cancelled for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status408RequestTimeout, new { error = "Request timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during risk analysis for ContractId: {ContractId}", request?.ContractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }

    // ------------------------------------------------------------------
    // New: Hangfire-backed contract-level analysis
    // ------------------------------------------------------------------

    /// <summary>
    /// Enqueue an AI analysis job for the specified contract.
    /// Validates contract existence and that the authenticated user owns the contract.
    /// Returns 202 Accepted when the job is successfully enqueued.
    /// </summary>
    [HttpPost("contracts/{contractId}/analyze")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeContractById(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        if (contractId == Guid.Empty)
            return BadRequest(new { error = "contractId must be a valid GUID" });

        var userId = _currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Unauthorized(new { error = "Not authenticated." });

        var contract = await _contractContext.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            return NotFound(new { error = $"Contract '{contractId}' was not found." });

        if (contract.OwnerId != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: you do not own this contract." });

        _logger.LogInformation("Enqueueing AI analysis job for ContractId: {ContractId} by UserId: {UserId}", contractId, userId);

        // Enqueue via DI-resolved service; does not capture scoped instance.
        // CancellationToken for the job is default — Hangfire controls job lifetime.
        _backgroundJobClient.Enqueue<IAIAnalysisJobService>(
            svc => svc.AnalyzeContractAsync(contractId, CancellationToken.None));

        return Accepted(new { contractId, message = "Analysis job enqueued." });
    }

    /// <summary>
    /// Retrieve the latest persisted AI analysis result for the specified contract.
    /// Enforces ownership; returns 404 when no analysis exists.
    /// </summary>
    [HttpGet("contracts/{contractId}/analysis")]
    [ProducesResponseType(typeof(AiAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiAnalysisResultDto>> GetAnalysis(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        if (contractId == Guid.Empty)
            return BadRequest(new { error = "contractId must be a valid GUID" });

        var userId = _currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Unauthorized(new { error = "Not authenticated." });

        var contract = await _contractContext.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract is null)
            return NotFound(new { error = $"Contract '{contractId}' was not found." });

        if (contract.OwnerId != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: you do not own this contract." });

        var entity = await _aiContext.AiAnalysisResults
            .AsNoTracking()
            .Where(r => r.ContractId == contractId)
            .OrderByDescending(r => r.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return NotFound(new { error = $"No analysis found for contract '{contractId}'." });

        var dto = new AiAnalysisResultDto
        {
            Id = entity.Id,
            ContractId = entity.ContractId,
            Summary = entity.Summary,
            ExtractedValue = entity.ExtractedValue,
            ExtractedExpiryDate = entity.ExtractedExpiryDate,
            RiskFlags = entity.RiskFlags,
            AnalyzedAt = entity.AnalyzedAt
        };

        return Ok(dto);
    }

    // ------------------------------------------------------------------
    // Minimal no-op fakes supporting the legacy 2-arg constructor path.
    // Keeps existing unit tests (auth attribute checks) green without
    // forcing them to supply Hangfire/DB dependencies.
    // ------------------------------------------------------------------
    private sealed class FakeContractContext : IContractManagementDbContext
    {
        public DbSet<ContractManagement.Domain.Contract.Entities.ContractType> ContractTypes => throw new NotImplementedException();
        public DbSet<ContractManagement.Domain.Contract.Entities.ContractTemplateVersion> ContractTemplateVersions => throw new NotImplementedException();
        public DbSet<ContractManagement.Domain.Contract.Entities.Contract> Contracts => throw new NotImplementedException();
        public DbSet<ContractManagement.Domain.Workflow.Entities.WorkflowDefinition> WorkflowDefinitions => throw new NotImplementedException();
        public DbSet<ContractManagement.Domain.Workflow.Entities.WorkflowStep> WorkflowSteps => throw new NotImplementedException();
        public DbSet<ContractManagement.Domain.Workflow.Entities.ApprovalStep> ApprovalSteps => throw new NotImplementedException();
        public DbSet<ContractManagement.Domain.Workflow.Entities.Signature> Signatures => throw new NotImplementedException();
        public Task<Guid> GetDefaultApproverIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(Guid.Empty);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeAiContext : IAiDbContext
    {
        public DbSet<ContractManagement.Domain.AI.Entities.AiAnalysisResult> AiAnalysisResults => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => null;
        public string? Role => null;
        public bool IsAuthenticated => false;
    }

    private sealed class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public string Create(Hangfire.Common.Job job, Hangfire.States.IState state) => throw new NotImplementedException();
        public bool ChangeState(string jobId, Hangfire.States.IState state) => throw new NotImplementedException();
        public bool ChangeState(string jobId, Hangfire.States.IState state, string? expectedState) => throw new NotImplementedException();
    }
}
