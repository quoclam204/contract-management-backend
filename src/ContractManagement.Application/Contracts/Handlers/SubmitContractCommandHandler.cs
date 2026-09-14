using ContractManagement.Application.Contracts.Commands;
using ContractManagement.Application.Contracts.Interfaces;
using MediatR;

namespace ContractManagement.Application.Contracts.Handlers;

/// <summary>
/// Handler xử lý SubmitContractCommand qua MediatR pipeline.
/// </summary>
public class SubmitContractCommandHandler : IRequestHandler<SubmitContractCommand>
{
    private readonly IContractService _contractService;

    public SubmitContractCommandHandler(IContractService contractService)
    {
        _contractService = contractService;
    }

    public async Task Handle(SubmitContractCommand request, CancellationToken cancellationToken)
    {
        await _contractService.SubmitContractAsync(request.ContractId);
    }
}
