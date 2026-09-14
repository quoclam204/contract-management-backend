using ContractManagement.Domain.Identity.Enums;

namespace ContractManagement.Application.Identity.DTOs;

/// <summary>
/// DTO chứa thông tin cập nhật người dùng (Admin)
/// </summary>
public record UpdateUserDto(
    string FullName,
    string Email,
    UserRole Role,
    Guid? DepartmentId = null,
    bool? IsActive = null
);

/// <summary>
/// DTO chứa các tham số lọc và phân trang danh sách người dùng
/// </summary>
public class UserFilterDto
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }

    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO thông tin chi tiết người dùng kèm theo tên phòng ban
/// </summary>
public record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string RoleName,
    Guid? DepartmentId,
    string? DepartmentName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
