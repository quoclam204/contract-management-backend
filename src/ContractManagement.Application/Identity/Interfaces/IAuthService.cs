using ContractManagement.Application.Identity.DTOs;

namespace ContractManagement.Application.Identity.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserDto> RegisterAsync(RegisterUserDto request, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(Guid userId, string fullName, CancellationToken cancellationToken = default);
}
