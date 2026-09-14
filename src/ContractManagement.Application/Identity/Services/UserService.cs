using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Common.Models;
using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Identity.Services;

/// <summary>
/// Service xử lý các nghiệp vụ quản lý người dùng
/// </summary>
public class UserService : IUserService
{
    private readonly IIdentityDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UserService(IIdentityDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<UserDetailDto>> GetUsersPagedAsync(UserFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .AsQueryable();

        // 1. Lọc theo trạng thái hoạt động
        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        // 2. Lọc theo vai trò (Role)
        if (filter.Role.HasValue)
        {
            query = query.Where(u => u.Role == filter.Role.Value);
        }

        // 3. Lọc theo phòng ban (Department)
        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(u => u.DepartmentId == filter.DepartmentId.Value);
        }

        // 4. Tìm kiếm từ khóa (FullName hoặc Email)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }

        // 5. Đếm tổng số bản ghi thỏa điều kiện lọc
        var totalCount = await query.CountAsync(cancellationToken);

        // 6. Sắp xếp, phân trang và lấy dữ liệu
        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(ToDetailDto).ToList();

        return new PagedResult<UserDetailDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user != null ? ToDetailDto(user) : null;
    }

    public async Task<UserDetailDto?> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ArgumentException("Full name is required.", nameof(dto.FullName));

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.", nameof(dto.Email));

        var user = await _dbContext.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            return null;

        var emailNormalized = dto.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Users
            .AnyAsync(u => u.Id != id && u.Email.ToLower() == emailNormalized, cancellationToken);

        if (emailExists)
            throw new InvalidOperationException($"User with email '{dto.Email}' already exists.");

        if (dto.DepartmentId.HasValue)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(d => d.Id == dto.DepartmentId.Value, cancellationToken);
            if (!deptExists)
                throw new ArgumentException("Specified department does not exist.", nameof(dto.DepartmentId));
        }

        // Cập nhật thông tin
        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim();
        user.Role = dto.Role;
        user.DepartmentId = dto.DepartmentId;

        if (dto.IsActive.HasValue)
        {
            // Business Rule: Không cho phép tự khóa tài khoản của chính mình
            if (!dto.IsActive.Value && _currentUserService.UserId == id)
                throw new InvalidOperationException("You cannot deactivate your own account.");

            user.IsActive = dto.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Load lại thông tin Department mới nhất nếu có thay đổi
        if (user.DepartmentId.HasValue)
        {
            user.Department = await _dbContext.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value, cancellationToken);
        }
        else
        {
            user.Department = null;
        }

        return ToDetailDto(user);
    }

    public async Task<bool> DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId == id)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleUserActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            return false;

        // Nếu tài khoản đang active mà muốn chuyển sang inactive thì không được tự khóa chính mình
        if (user.IsActive && _currentUserService.UserId == id)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<UserDetailDto>> GetUsersByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.DepartmentId == departmentId)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        return users.Select(ToDetailDto).ToList();
    }

    private static UserDetailDto ToDetailDto(User user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.Role.ToString(),
            user.DepartmentId,
            user.Department != null ? user.Department.Name : null,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        );
}
