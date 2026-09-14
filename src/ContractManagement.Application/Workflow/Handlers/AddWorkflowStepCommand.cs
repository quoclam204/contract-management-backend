using ContractManagement.Application.Workflow.DTOs;
using MediatR;

namespace ContractManagement.Application.Workflow.Handlers;

public class AddWorkflowStepCommand : IRequest<WorkflowStepDto>
{
    public Guid WorkflowDefinitionId { get; set; }
    public CreateWorkflowStepRequest Step { get; set; } = default!;
}