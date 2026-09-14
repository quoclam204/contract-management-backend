using ContractManagement.Domain.Identity.Enums;

namespace ContractManagement.Domain.Identity.Entities;

/// <summary>
/// Thực thể người dùng hệ thống (Khớp bảng dbo.USERS trong database.sql)
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }

    public User()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public User(string fullName, string email, string passwordHash, UserRole role, Guid? departmentId = null) : this()
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        DepartmentId = departmentId;
    }
}
