using ContractManagement.Domain.Notification.Enums;

namespace ContractManagement.Application.Notification.DTOs;

/// <summary>
/// DTO for returning notification information
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ContractId { get; set; }
    public NotificationType Type { get; set; }
    public string TypeName => Type.ToString();
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating a notification
/// </summary>
public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public Guid? ContractId { get; set; }
    public NotificationType Type { get; set; }
}

/// <summary>
/// Request DTO for marking notifications as read
/// </summary>
public class MarkNotificationsReadRequest
{
    public List<Guid> NotificationIds { get; set; } = new();
}