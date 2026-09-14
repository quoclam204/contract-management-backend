namespace ContractManagement.Application.Common.Interfaces;

/// <summary>
/// Interface cung cấp thông tin người dùng hiện tại đang đăng nhập.
/// Các module khác (Contract, Workflow, Partner, etc.) inject interface này để lấy UserId/Role.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
