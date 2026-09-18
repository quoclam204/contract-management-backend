using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Identity.Services;

public class AuthService : IAuthService
{
    private readonly IIdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IIdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email and password are required.");

        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == emailNormalized, cancellationToken);

        // Auto-provision standard test accounts if they don't exist yet in the database
        if (user == null)
        {
            if (emailNormalized == "admin@gmail.com" && request.Password == "admin@2004")
            {
                user = new User("Quản Trị Viên (Admin)", "admin@gmail.com", _passwordHasher.HashPassword("admin@2004"), UserRole.Admin, null);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (emailNormalized == "user1@clm.com" && request.Password == "Password@123")
            {
                user = new User("Nguyễn Văn User 1", "user1@clm.com", _passwordHasher.HashPassword("Password@123"), UserRole.Staff, null);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (emailNormalized == "admin@clm.com" && request.Password == "Password@123")
            {
                user = new User("Quản Trị Hệ Thống", "admin@clm.com", _passwordHasher.HashPassword("Password@123"), UserRole.Admin, null);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (emailNormalized == "manager@clm.com" && request.Password == "Password@123")
            {
                user = new User("Trần Thị Manager", "manager@clm.com", _passwordHasher.HashPassword("Password@123"), UserRole.Manager, null);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (emailNormalized == "approver@clm.com" && request.Password == "Password@123")
            {
                user = new User("Lê Văn Approver", "approver@clm.com", _passwordHasher.HashPassword("Password@123"), UserRole.Approver, null);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user);
        var userDto = ToDto(user);

        return new LoginResponseDto(token, userDto, refreshToken);
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token is required.");

        var principal = _jwtTokenService.GetPrincipalFromToken(refreshToken, validateLifetime: true);
        if (principal == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var tokenType = principal.FindFirst("token_type")?.Value;
        if (tokenType != "refresh")
            throw new UnauthorizedAccessException("Token is not a valid refresh token.");

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token claims.");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        var newAccessToken = _jwtTokenService.GenerateToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user);
        var userDto = ToDto(user);

        return new LoginResponseDto(newAccessToken, userDto, newRefreshToken);
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.", nameof(request.FullName));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.", nameof(request.Email));

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.", nameof(request.Password));

        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email == emailNormalized, cancellationToken);

        if (emailExists)
            throw new InvalidOperationException($"User with email '{request.Email}' already exists.");

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken);
            if (!deptExists)
                throw new ArgumentException("Specified department does not exist.", nameof(request.DepartmentId));
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        // Public registration always assigns Staff role to prevent privilege escalation
        var user = new User(
            request.FullName.Trim(),
            request.Email.Trim(),
            passwordHash,
            UserRole.Staff,
            request.DepartmentId
        );

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.", nameof(request.FullName));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.", nameof(request.Email));

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.", nameof(request.Password));

        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email == emailNormalized, cancellationToken);

        if (emailExists)
            throw new InvalidOperationException($"User with email '{request.Email}' already exists.");

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken);
            if (!deptExists)
                throw new ArgumentException("Specified department does not exist.", nameof(request.DepartmentId));
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(
            request.FullName.Trim(),
            request.Email.Trim(),
            passwordHash,
            request.Role,
            request.DepartmentId
        );

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.Role.ToString(),
                u.DepartmentId,
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user != null ? ToDto(user) : null;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new ArgumentException("Current password is required.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new ArgumentException("New password must be at least 6 characters.");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không chính xác.");

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, string fullName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty.");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        user.FullName = fullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.Role.ToString(), user.DepartmentId, user.IsActive, user.CreatedAt);
}
