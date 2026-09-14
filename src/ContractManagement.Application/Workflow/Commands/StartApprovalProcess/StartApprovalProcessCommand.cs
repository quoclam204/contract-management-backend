using MediatR;

namespace ContractManagement.Application.Workflow.Commands.StartApprovalProcess;

/// <summary>
/// Command to start the approval workflow for a contract.
/// </summary>
public sealed record StartApprovalProcessCommand(Guid ContractId) : IRequest<Unit>;
