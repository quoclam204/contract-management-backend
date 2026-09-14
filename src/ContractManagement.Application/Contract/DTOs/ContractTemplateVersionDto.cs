using ContractManagement.Domain.Workflow.Enums;

namespace ContractManagement.Application.Contract.DTOs;

public class ContractTemplateVersionDto
{
    public Guid Id { get; set; }
    public Guid ContractTypeId { get; set; }
    public int Version { get; set; }
    public string? TemplateFileUrl { get; set; }
    public string? ContentJson { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
