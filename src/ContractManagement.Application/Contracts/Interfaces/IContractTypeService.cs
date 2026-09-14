using ContractManagement.Application.Contracts.DTOs;

namespace ContractManagement.Application.Contracts.Interfaces;

/// <summary>
/// Interface for contract type management service.
/// </summary>
public interface IContractTypeService
{
    Task<ContractTypeDto> CreateContractTypeAsync(CreateContractTypeRequest request);
    Task<ContractTypeDto?> GetContractTypeByIdAsync(Guid id);
    Task<List<ContractTypeDto>> GetContractTypesAsync();
    Task<ContractTypeDto> UpdateContractTypeAsync(Guid id, UpdateContractTypeRequest request);
    Task<bool> DeleteContractTypeAsync(Guid id);
}