namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Request model for updating an existing contract type.
/// </summary>
public class UpdateContractTypeRequest
{
    public string? Name { get; set; }
}