using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ContractManagement.Api.Controllers.AI;

/// <summary>
/// AI Contract Assistant API endpoints for contract analysis operations.
/// Provides endpoints for extracting contract information, summarization, and risk analysis.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("AI Contract Assistant")]
[Authorize]
public class AIContractAssistantController : ControllerBase
{
    private readonly IAIContractAssistantService _aiService;
    private readonly ILogger<AIContractAssistantController> _logger;

    public AIContractAssistantController(
        IAIContractAssistantService aiService,
        ILogger<AIContractAssistantController> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extract key information from a contract using AI analysis.
    ///
    /// Extracts structured information such as:
    /// - Contract number, title, parties involved
    /// - Contract type, value, key dates
    /// - Status and other contract identifiers
    /// </summary>
    /// <param name="request">Request containing contract ID and optional content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted contract information</returns>
    /// <response code="200">Successfully extracted contract information</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during extraction</response>
    /// <remarks>
    /// Returns 200 with ExtractedContractInfoDto on success,
    /// 400 for invalid request, or 500 for server errors.
    /// </remarks>
    [HttpPost("extract")]
    public async Task<ActionResult<ExtractedContractInfoDto>> ExtractContractInfo(
        [FromBody] ExtractContractRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request?.ContractId == Guid.Empty)
                return BadRequest(new { error = "ContractId must be a valid GUID" });

            _logger.LogInformation("Extracting contract information for ContractId: {ContractId}", request.ContractId);

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
    ///
    /// Produces:
    /// - Executive summary of contract terms and conditions
    /// - Key points/highlights for quick reference
    /// </summary>
    /// <param name="request">Request containing contract ID and content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contract summary with key points</returns>
    /// <response code="200">Successfully generated summary</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during summarization</response>
    /// <remarks>
    /// Returns 200 with ContractSummaryDto on success,
    /// 400 for invalid request, or 500 for server errors.
    /// </remarks>
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
    ///
    /// Identifies risks such as:
    /// - Unfavorable terms or clauses
    /// - Compliance and legal risks
    /// - Financial or operational risks
    ///
    /// Each risk is categorized by severity level (Low=0, Medium=1, High=2).
    /// </summary>
    /// <param name="request">Request containing contract ID and content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risk analysis result with identified risks</returns>
    /// <response code="200">Successfully completed risk analysis</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during analysis</response>
    /// <remarks>
    /// Returns 200 with ContractRiskAnalysisDto on success,
    /// 400 for invalid request, or 500 for server errors.
    /// </remarks>
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
}
