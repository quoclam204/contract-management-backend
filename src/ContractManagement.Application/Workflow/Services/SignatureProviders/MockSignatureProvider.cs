using System.Security.Cryptography;
using System.Text;
using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Application.Workflow.Services.SignatureProviders;

/// <summary>
/// Triển khai MVP: Giả lập ký điện tử nội bộ nhanh chóng cho kiểm thử và môi trường dev
/// </summary>
public class MockSignatureProvider : ISignatureProvider
{
    public SignatureMethod Method => SignatureMethod.Mock;

    public Task<SignatureResult> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty)
            return Task.FromResult(SignatureResult.Fail("ContractId không hợp lệ."));

        if (request.SignerId == Guid.Empty)
            return Task.FromResult(SignatureResult.Fail("SignerId không hợp lệ."));

        if (string.IsNullOrWhiteSpace(request.SignerName))
            return Task.FromResult(SignatureResult.Fail("Tên người ký không được để trống."));

        var rawData = $"MOCK:{request.ContractId:N}:{request.SignerId:N}:{(byte)request.SignerType}:{request.Timestamp:yyyyMMddHHmmss}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        var signatureHash = $"mock_sig_{Convert.ToHexString(hashBytes).ToLowerInvariant()}";

        return Task.FromResult(SignatureResult.Ok(signatureHash));
    }

    public Task<bool> VerifyAsync(string signatureHash, string dataToVerify, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signatureHash) || !signatureHash.StartsWith("mock_sig_"))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
