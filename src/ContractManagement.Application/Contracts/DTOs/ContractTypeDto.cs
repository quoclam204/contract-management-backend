namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Data Transfer Object for ContractType entity.
/// </summary>
public class ContractTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
