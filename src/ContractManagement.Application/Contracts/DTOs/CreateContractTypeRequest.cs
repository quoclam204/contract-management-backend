namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Request model for creating a new contract type.
/// </summary>
public class CreateContractTypeRequest
{
    public string Name { get; set; } = string.Empty;
}