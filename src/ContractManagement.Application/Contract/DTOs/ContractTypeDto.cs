namespace ContractManagement.Application.Contract.DTOs;

public class ContractTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
