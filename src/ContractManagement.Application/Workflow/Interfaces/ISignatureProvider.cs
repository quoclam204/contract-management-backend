using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Application.Workflow.Interfaces;

/// <summary>
/// Interface trừu tượng hóa cho các nhà cung cấp chữ ký điện tử (Mock, OTP, DigitalCA)
/// </summary>
public interface ISignatureProvider
{
    SignatureMethod Method { get; }
    Task<SignatureResult> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(string signatureHash, string dataToVerify, CancellationToken cancellationToken = default);
}
