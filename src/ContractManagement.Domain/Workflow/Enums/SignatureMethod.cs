namespace ContractManagement.Domain.Workflow.Enums;

/// <summary>
/// Phương thức ký: 0 = Mock (Giả lập), 1 = OTP (Mã xác thực), 2 = DigitalCA (Chữ ký số CA)
/// </summary>
public enum SignatureMethod : byte
{
    Mock = 0,
    Otp = 1,
    DigitalCa = 2
}
