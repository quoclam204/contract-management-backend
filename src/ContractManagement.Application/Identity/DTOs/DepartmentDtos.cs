namespace ContractManagement.Application.Identity.DTOs;

public record DepartmentDto(
    Guid Id,
    string Name,
    Guid? ManagerId,
    DateTime CreatedAt
);

public record CreateDepartmentDto(
    string Name,
    Guid? ManagerId = null
);

public record UpdateDepartmentDto(
    string Name,
    Guid? ManagerId = null
);
