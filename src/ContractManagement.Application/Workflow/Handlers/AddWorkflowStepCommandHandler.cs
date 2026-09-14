using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using MediatR;

namespace ContractManagement.Application.Workflow.Handlers;

/// <summary>
/// Handler for AddWorkflowStepCommand.
/// </summary>
public class AddWorkflowStepCommandHandler : IRequestHandler<AddWorkflowStepCommand, WorkflowStepDto>
{
    private readonly IWorkflowService _workflowService;

    public AddWorkflowStepCommandHandler(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    public async Task<WorkflowStepDto> Handle(AddWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        return await _workflowService.AddStepAsync(request.WorkflowDefinitionId, request.Step);
    }
}