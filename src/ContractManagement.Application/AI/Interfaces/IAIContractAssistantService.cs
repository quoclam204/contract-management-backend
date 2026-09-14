using ContractManagement.Application.AI.DTOs;

namespace ContractManagement.Application.AI.Interfaces;

/// <summary>
/// Application service interface for AI-powered contract analysis.
/// Provides operations for extracting, summarizing, and analyzing risks in contracts.
///
/// Contract (abstraction) - implementation belongs in Infrastructure layer.
/// </summary>
public interface IAIContractAssistantService
{
    /// <summary>
    /// Extract key information from a contract using AI analysis.
    ///
    /// Extracts structured information such as:
    /// - Contract number, title, parties involved
    /// - Contract type, value, key dates
    /// - Status and other contract identifiers
    /// </summary>
    /// <param name="request">Extract request containing contract ID and optional content</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Extracted contract information DTO</returns>
    /// <exception cref="ArgumentException">When ContractId is invalid</exception>
    /// <exception cref="InvalidOperationException">When contract cannot be retrieved or processed</exception>
    Task<ExtractedContractInfoDto> ExtractContractInfoAsync(
        ExtractContractRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a concise summary of a contract using AI analysis.
    ///
    /// Produces:
    /// - Executive summary of contract terms and conditions
    /// - Key points/highlights for quick reference
    /// </summary>
    /// <param name="request">Summarize request containing contract ID and content</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Contract summary with key points DTO</returns>
    /// <exception cref="ArgumentException">When request data is invalid or insufficient</exception>
    /// <exception cref="InvalidOperationException">When summary generation fails</exception>
    Task<ContractSummaryDto> SummarizeContractAsync(
        SummarizeContractRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze a contract for potential risks and issues using AI.
    ///
    /// Identifies risks such as:
    /// - Unfavorable terms or clauses
    /// - Compliance and legal risks
    /// - Financial or operational risks
    ///
    /// Each risk is categorized by severity level (Low, Medium, High).
    /// </summary>
    /// <param name="request">Risk analysis request containing contract ID and content</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Risk analysis result with identified risks DTO</returns>
    /// <exception cref="ArgumentException">When request data is invalid or insufficient</exception>
    /// <exception cref="InvalidOperationException">When analysis fails</exception>
    Task<ContractRiskAnalysisDto> AnalyzeContractRiskAsync(
        AnalyzeContractRiskRequest request,
        CancellationToken cancellationToken = default);
}
