using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Application.Workflow.DTOs;

public class CreateSignatureRequest
{
    public Guid ContractId { get; set; }
    public SignerType SignerType { get; set; }
    public Guid? InternalSignerId { get; set; }
    public Guid? PartnerSignerId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public SignatureMethod SignatureMethod { get; set; } = SignatureMethod.Mock;
    public string? OtpCode { get; set; }
}

public class SignatureDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public SignerType SignerType { get; set; }
    public string SignerTypeName => SignerType.ToString();
    public Guid? InternalSignerId { get; set; }
    public Guid? PartnerSignerId { get; set; }
    public string SignerNameSnapshot { get; set; } = string.Empty;
    public SignatureMethod SignatureMethod { get; set; }
    public string SignatureMethodName => SignatureMethod.ToString();
    public DateTime SignedAt { get; set; }
    public string SignatureHash { get; set; } = string.Empty;
}

public class ContractSignatureStatusDto
{
    public Guid ContractId { get; set; }
    public bool HasInternalSignature { get; set; }
    public bool HasPartnerSignature { get; set; }
    public bool IsFullySigned { get; set; }
    public int TotalRequiredParties { get; set; } = 2;
    public int SignedPartiesCount { get; set; }
    public List<SignatureDto> Signatures { get; set; } = new();
}

public class SignDocumentRequest
{
    public Guid ContractId { get; set; }
    public Guid SignerId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public SignerType SignerType { get; set; }
    public string? OtpCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SignatureResult
{
    public bool Success { get; set; }
    public string SignatureHash { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public static SignatureResult Ok(string hash) => new() { Success = true, SignatureHash = hash };
    public static SignatureResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
