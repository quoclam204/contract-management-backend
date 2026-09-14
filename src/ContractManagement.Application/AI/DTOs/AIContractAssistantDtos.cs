namespace ContractManagement.Application.AI.DTOs;

/// <summary>
/// Request for extracting contract information
/// </summary>
public class ExtractContractRequest
{
    /// <summary>
    /// Contract ID to extract from
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// Contract content (raw text) if available, otherwise will be fetched from storage
    /// </summary>
    public string? ContractContent { get; set; }
}

/// <summary>
/// DTO representing extracted contract information
/// Serves as a contract for AI response format
/// </summary>
public class ExtractedContractInfoDto
{
    public string? ContractNumber { get; set; }
    public string? Title { get; set; }
    public string? Partner { get; set; }
    public string? ContractType { get; set; }
    public decimal? Value { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Request for summarizing a contract
/// </summary>
public class SummarizeContractRequest
{
    /// <summary>
    /// Contract ID to summarize
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// Contract content (full text) for analysis
    /// </summary>
    public string ContractContent { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing contract summary result
/// </summary>
public class ContractSummaryDto
{
    /// <summary>
    /// Contract ID
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// AI-generated summary of the contract
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Key points extracted from contract
    /// </summary>
    public List<string> KeyPoints { get; set; } = new();
}

/// <summary>
/// Represents a single risk item identified in contract
/// </summary>
public class ContractRiskDto
{
    /// <summary>
    /// Risk title/heading
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the risk
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Risk severity level
    /// </summary>
    public RiskLevel Level { get; set; }

    /// <summary>
    /// Risk level name for display purposes
    /// </summary>
    public string LevelName => Level.ToString();
}

/// <summary>
/// Request for analyzing contract risks
/// </summary>
public class AnalyzeContractRiskRequest
{
    /// <summary>
    /// Contract ID to analyze
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// Contract content (full text) for risk analysis
    /// </summary>
    public string ContractContent { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing risk analysis result for a contract
/// </summary>
public class ContractRiskAnalysisDto
{
    /// <summary>
    /// Contract ID
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// List of identified risks
    /// </summary>
    public List<ContractRiskDto> Risks { get; set; } = new();
}

/// <summary>
/// Enum representing risk severity levels
/// </summary>
public enum RiskLevel : byte
{
    Low = 0,
    Medium = 1,
    High = 2
}
