using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Application.Workflow.Services.SignatureProviders;

/// <summary>
/// Triển khai MVP: Ký điện tử xác thực qua mã OTP nội bộ
/// </summary>
public class OtpSignatureProvider : ISignatureProvider
{
    private static readonly Regex OtpRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    public SignatureMethod Method => SignatureMethod.Otp;

    public Task<SignatureResult> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty)
            return Task.FromResult(SignatureResult.Fail("ContractId không hợp lệ."));

        if (request.SignerId == Guid.Empty)
            return Task.FromResult(SignatureResult.Fail("SignerId không hợp lệ."));

        if (string.IsNullOrWhiteSpace(request.SignerName))
            return Task.FromResult(SignatureResult.Fail("Tên người ký không được để trống."));

        if (string.IsNullOrWhiteSpace(request.OtpCode))
            return Task.FromResult(SignatureResult.Fail("Mã OTP không được để trống khi chọn ký bằng phương thức OTP."));

        var trimmedOtp = request.OtpCode.Trim();

        if (!OtpRegex.IsMatch(trimmedOtp))
            return Task.FromResult(SignatureResult.Fail("Mã OTP phải bao gồm chính xác 6 chữ số."));

        // Giả lập mã OTP '000000' đại diện cho mã OTP đã hết hạn hoặc sai
        if (trimmedOtp == "000000")
            return Task.FromResult(SignatureResult.Fail("Mã OTP không chính xác hoặc đã hết hiệu lực."));

        var rawData = $"OTP:{request.ContractId:N}:{request.SignerId:N}:{(byte)request.SignerType}:{trimmedOtp}:{request.Timestamp:yyyyMMddHHmmss}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        var signatureHash = $"otp_sig_{Convert.ToHexString(hashBytes).ToLowerInvariant()}";

        return Task.FromResult(SignatureResult.Ok(signatureHash));
    }

    public Task<bool> VerifyAsync(string signatureHash, string dataToVerify, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signatureHash) || !signatureHash.StartsWith("otp_sig_"))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
