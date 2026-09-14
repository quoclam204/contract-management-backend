using MediatR;

namespace ContractManagement.Application.Contracts.Commands;

/// <summary>
/// Command to submit a contract for approval.
/// </summary>
/// <param name="ContractId">The ID of the contract to submit</param>
public record SubmitContractCommand(Guid ContractId) : IRequest;