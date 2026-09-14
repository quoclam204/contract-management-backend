using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Application.Workflow.DTOs;

public class WorkflowStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public int StepOrder { get; set; }
    public ApproverRole ApproverRole { get; set; }
    public string ApproverRoleName => ApproverRole.ToString();
    public bool IsRequired { get; set; }
    public decimal MinimumAmount { get; set; }
}

public class WorkflowDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ConditionExpression { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkflowStepDto> Steps { get; set; } = new();
}

public class CreateWorkflowStepRequest
{
    public int StepOrder { get; set; }
    public ApproverRole ApproverRole { get; set; }
    public bool IsRequired { get; set; } = true;
    public decimal MinimumAmount { get; set; } = 0;
}

public class CreateWorkflowDefinitionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ConditionExpression { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateWorkflowStepRequest> Steps { get; set; } = new();
}

public class CreateWorkflowVersionRequest
{
    public string? ConditionExpression { get; set; }
    public List<CreateWorkflowStepRequest> Steps { get; set; } = new();
}

public class UpdateWorkflowDefinitionRequest
{
    public string? ConditionExpression { get; set; }
    public bool? IsActive { get; set; }
}

public class ResolveWorkflowRequest
{
    public decimal ContractValue { get; set; }
}

public class ResolveWorkflowResponse
{
    public bool IsMatched { get; set; }
    public string Message { get; set; } = string.Empty;
    public WorkflowDefinitionDto? Workflow { get; set; }
}

public class EvaluateConditionRequest
{
    public string Expression { get; set; } = string.Empty;
    public decimal ContractValue { get; set; }
}

public class EvaluateConditionResponse
{
    public bool IsValidSyntax { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSatisfied { get; set; }
}