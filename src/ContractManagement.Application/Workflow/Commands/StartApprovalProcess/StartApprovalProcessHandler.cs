using ContractManagement.Application.Workflow.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ContractManagement.Application.Workflow.Commands.StartApprovalProcess;

public sealed class StartApprovalProcessHandler : IRequestHandler<StartApprovalProcessCommand, Unit>
{
    private readonly IApprovalService _approvalService;

    public StartApprovalProcessHandler(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    public async Task<Unit> Handle(StartApprovalProcessCommand request, CancellationToken cancellationToken)
    {
        // We need to fetch contract to get ContractValue for the service.
        // However IApprovalService does not expose a method to get contract details.
        // We'll need to inject IContractRepository or rely on the service to fetch internally.
        // Since we don't have that, we'll adjust: we can call a service method that takes only ContractId
        // and lets the service fetch the contract.
        // But the existing SubmitForApprovalAsync expects SubmitContractApprovalRequest with ContractValue.
        // We'll create a wrapper service method or we can inject the DbContext.
        // For simplicity, we'll inject IApprovalService and also IContractRepository.
        // However to avoid changing the service interface, we'll create a new method in the service?
        // Instead, we can let the handler use the service's internal logic by duplicating?
        // Better to modify IApprovalService to have an overload that takes only ContractId?
        // But we cannot change existing interfaces? The task does not forbid it.
        // Let's assume we can add a new method to IApprovalService: StartApprovalProcessAsync(Guid contractId)
        // However we must keep existing methods for backward compatibility.
        // Since we are in a feature branch, we can extend the interface.
        // Let's do that: we'll add a new method to IApprovalService and implement it in ApprovalService.
        // Then the handler will call that new method.
        // For now, we'll throw NotImplementedException to see the compilation.
        throw new System.NotImplementedException();
    }
}
