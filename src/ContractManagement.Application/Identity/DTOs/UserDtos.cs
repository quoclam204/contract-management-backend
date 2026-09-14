using ContractManagement.Domain.Identity.Enums;

namespace ContractManagement.Application.Identity.DTOs;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string RoleName,
    Guid? DepartmentId,
    bool IsActive,
    DateTime CreatedAt
);

public record RegisterUserDto(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    Guid? DepartmentId = null
);

public record LoginRequestDto(
    string Email,
    string Password
);

public record LoginResponseDto(
    string Token,
    UserDto User
);
