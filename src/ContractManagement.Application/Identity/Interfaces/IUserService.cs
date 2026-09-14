using ContractManagement.Application.Common.Models;
using ContractManagement.Application.Identity.DTOs;

namespace ContractManagement.Application.Identity.Interfaces;

/// <summary>
/// Interface quản trị người dùng (User Management CRUD, Pagination & Filtering)
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lấy danh sách người dùng có phân trang và lọc theo điều kiện
    /// </summary>
    Task<PagedResult<UserDetailDto>> GetUsersPagedAsync(UserFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết thông tin người dùng theo Id (kèm tên phòng ban)
    /// </summary>
    Task<UserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin người dùng
    /// </summary>
    Task<UserDetailDto?> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vô hiệu hóa (khóa) tài khoản người dùng
    /// </summary>
    Task<bool> DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của tài khoản (Toggle Active)
    /// </summary>
    Task<bool> ToggleUserActiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách người dùng theo phòng ban
    /// </summary>
    Task<IEnumerable<UserDetailDto>> GetUsersByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
