namespace ContractManagement.Application.AI.DTOs;

/// <summary>
/// Response DTO for persisted AI analysis result (AI_ANALYSIS_RESULTS table).
/// Does NOT expose domain entity / EF tracking directly.
/// Note: ContractType and SignedDate are intentionally absent — no such columns exist.
/// </summary>
public class AiAnalysisResultDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string? Summary { get; set; }
    public decimal? ExtractedValue { get; set; }
    public DateTime? ExtractedExpiryDate { get; set; }
    public string? RiskFlags { get; set; }
    public DateTime AnalyzedAt { get; set; }
}
