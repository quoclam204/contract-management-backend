using ContractManagement.Application.Identity.DTOs;

namespace ContractManagement.Application.Identity.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> RegisterAsync(RegisterUserDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
