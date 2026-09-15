namespace ContractManagement.Application.AI.Interfaces;

/// <summary>
/// Application job service for AI contract analysis.
/// Orchestrates file retrieval, text extraction, AI extraction/summarization,
/// and persistence of <see cref="Domain.AI.Entities.AiAnalysisResult"/>.
/// Designed to be invoked by Hangfire (or any scheduler) without coupling
/// the Application layer to Hangfire types.
/// </summary>
public interface IAIAnalysisJobService
{
    /// <summary>
    /// Analyzes the contract identified by <paramref name="contractId"/>:
    /// downloads its file, extracts text, calls AI services, and persists the result.
    /// </summary>
    /// <param name="contractId">Target contract identifier.</param>
    /// <param name="cancellationToken">Cancellation token propagated to all async operations.</param>
    /// <exception cref="ArgumentException">When <paramref name="contractId"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">When contract not found, file missing, or document empty.</exception>
    /// <exception cref="NotSupportedException">When document format is unsupported.</exception>
    Task AnalyzeContractAsync(Guid contractId, CancellationToken cancellationToken = default);
}
