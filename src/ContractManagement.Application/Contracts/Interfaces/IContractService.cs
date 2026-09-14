using ContractManagement.Application.Contracts.DTOs;
using MediatR;

namespace ContractManagement.Application.Contracts.Interfaces;

/// <summary>
/// Interface for contract management service.
/// </summary>
public interface IContractService
{
    Task<ContractDto> CreateContractAsync(CreateContractRequest request);
    Task<ContractDto?> GetContractByIdAsync(Guid id);
    Task<List<ContractDto>> GetContractsAsync();
    Task<ContractDto> UpdateContractAsync(Guid id, UpdateContractRequest request);
    Task<bool> DeleteContractAsync(Guid id);
    Task SubmitContractAsync(Guid contractId);
    Task ApproveContractAsync(Guid contractId);
    Task SignContractAsync(Guid contractId);
    Task ActivateContractAsync(Guid contractId);
    Task ExpireContractAsync(Guid contractId);
    Task RenewContractAsync(Guid contractId);
    Task TerminateContractAsync(Guid contractId);
}