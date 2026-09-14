using System;

namespace ContractManagement.Domain.AI.Entities;

/// <summary>
/// Domain entity representing AI analysis results stored in AI_ANALYSIS_RESULTS table.
/// </summary>
public class AiAnalysisResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractId { get; set; }
    public string? Summary { get; set; }
    public decimal? ExtractedValue { get; set; }
    public DateTime? ExtractedExpiryDate { get; set; }
    public string? RiskFlags { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ContractManagement.Domain.Contract.Entities.Contract Contract { get; set; } = null!;

    public AiAnalysisResult()
    {
    }

    public AiAnalysisResult(
        Guid contractId,
        string? summary = null,
        decimal? extractedValue = null,
        DateTime? extractedExpiryDate = null,
        string? riskFlags = null)
    {
        if (contractId == Guid.Empty)
            throw new ArgumentException("ContractId must not be empty.", nameof(contractId));

        if (extractedValue.HasValue && extractedValue.Value < 0)
            throw new ArgumentException("ExtractedValue cannot be negative.", nameof(extractedValue));

        ContractId = contractId;
        Summary = summary;
        ExtractedValue = extractedValue;
        ExtractedExpiryDate = extractedExpiryDate;
        RiskFlags = riskFlags;
        AnalyzedAt = DateTime.UtcNow;
    }
}
