using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContractManagement.Domain.Notification.Enums;

namespace ContractManagement.Domain.Notification.Entities;

/// <summary>
/// Bảng NOTIFICATIONS: Thông báo cho người dùng
/// Type: 0=ApprovalRequest, 1=SignRequest, 2=ExpiringSoon, 3=SystemAnnouncement
/// ContractId có thể NULL để hỗ trợ thông báo hệ thống chung
/// </summary>
[Table("NOTIFICATIONS")]
public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public Guid? ContractId { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}