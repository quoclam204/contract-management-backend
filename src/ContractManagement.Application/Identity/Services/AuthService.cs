using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Domain.Identity.Entities;
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
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");

        var token = _jwtTokenService.GenerateToken(user);
        var userDto = ToDto(user);

        return new LoginResponseDto(token, userDto);
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
            .AnyAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);

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

    private static UserDto ToDto(User user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.Role.ToString(), user.DepartmentId, user.IsActive, user.CreatedAt);
}
