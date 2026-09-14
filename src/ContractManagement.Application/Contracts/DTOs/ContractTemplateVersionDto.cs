using ContractManagement.Domain.Workflow.Entities;

namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Data Transfer Object for ContractTemplateVersion entity.
/// </summary>
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