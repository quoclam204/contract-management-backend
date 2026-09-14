using ContractManagement.Application.Contracts.DTOs;

namespace ContractManagement.Application.Contracts.Interfaces;

/// <summary>
/// Interface for contract template version management service.
/// </summary>
public interface IContractTemplateVersionService
{
    Task<ContractTemplateVersionDto> CreateContractTemplateVersionAsync(CreateContractTemplateVersionRequest request);
    Task<ContractTemplateVersionDto?> GetContractTemplateVersionByIdAsync(Guid id);
    Task<List<ContractTemplateVersionDto>> GetContractTemplateVersionsAsync();
    Task<ContractTemplateVersionDto> UpdateContractTemplateVersionAsync(Guid id, UpdateContractTemplateVersionRequest request);
    Task<bool> DeleteContractTemplateVersionAsync(Guid id);
}