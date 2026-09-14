namespace ContractManagement.Domain.Notification.Enums;

/// <summary>
/// Loại thông báo: 0=ApprovalRequest, 1=SignRequest, 2=ExpiringSoon, 3=SystemAnnouncement
/// Khớp với CHECK constraint CK_NOTIFICATIONS_Type trong database.sql
/// </summary>
public enum NotificationType : byte
{
    ApprovalRequest = 0,
    SignRequest = 1,
    ExpiringSoon = 2,
    SystemAnnouncement = 3
}