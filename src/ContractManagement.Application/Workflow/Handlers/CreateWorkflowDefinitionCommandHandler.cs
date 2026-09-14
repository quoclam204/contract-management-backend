using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using MediatR;

namespace ContractManagement.Application.Workflow.Handlers;

/// <summary>
/// Handler for CreateWorkflowDefinitionCommand.
/// </summary>
public class CreateWorkflowDefinitionCommandHandler : IRequestHandler<CreateWorkflowDefinitionCommand, WorkflowDefinitionDto>
{
    private readonly IWorkflowService _workflowService;

    public CreateWorkflowDefinitionCommandHandler(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    public async Task<WorkflowDefinitionDto> Handle(CreateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateWorkflowDefinitionRequest
        {
            Name = request.Name,
            ConditionExpression = request.ConditionExpression,
            IsActive = request.IsActive,
            Steps = request.Steps
        };
        return await _workflowService.CreateDefinitionAsync(dto);
    }
}