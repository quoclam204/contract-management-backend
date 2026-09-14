using ContractManagement.Application.Workflow.DTOs;
using MediatR;

namespace ContractManagement.Application.Workflow.Handlers;

/// <summary>
/// Command to create a new workflow definition.
/// </summary>
public class CreateWorkflowDefinitionCommand : IRequest<WorkflowDefinitionDto>
{
    public string Name { get; set; } = string.Empty;
    public string? ConditionExpression { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateWorkflowStepRequest> Steps { get; set; } = new();
}