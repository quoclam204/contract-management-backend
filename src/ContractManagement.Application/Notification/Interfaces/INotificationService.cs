using ContractManagement.Application.Notification.DTOs;

namespace ContractManagement.Application.Notification.Interfaces;

/// <summary>
/// Interface for notification application operations
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification
    /// </summary>
    Task<NotificationDto> CreateAsync(CreateNotificationRequest request);

    /// <summary>
    /// Gets all notifications for a user, optionally filtered by read status
    /// </summary>
    Task<List<NotificationDto>> GetByUserIdAsync(Guid userId, bool? isRead = null);

    /// <summary>
    /// Gets unread notifications for a user
    /// </summary>
    Task<List<NotificationDto>> GetUnreadByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Marks specific notifications as read for a user
    /// </summary>
    Task<int> MarkAsReadAsync(Guid userId, List<Guid> notificationIds);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task<int> MarkAllAsReadAsync(Guid userId);
}