namespace ContractManagement.Domain.Identity.Enums;

/// <summary>
/// Vai trò người dùng (Khớp đúng database.sql: CK_USERS_Role BETWEEN 0 AND 3)
/// 0=Admin, 1=Manager (Trưởng phòng), 2=Staff (Nhân viên), 3=Approver (Người phê duyệt)
/// </summary>
public enum UserRole : byte
{
    Admin = 0,
    Manager = 1,
    Staff = 2,
    Approver = 3
}
