using MediatR;

namespace ContractManagement.Application.Events.Identity;

/// <summary>
/// Sự kiện bắn ra khi người dùng mới đăng ký tài khoản.
/// Module Notification sẽ lắng nghe sự kiện này để gửi email chào mừng.
/// </summary>
/// <param name="UserId">ID của người dùng vừa đăng ký</param>
public record UserRegisteredEvent(Guid UserId) : INotification;