namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Request model for creating a new contract template version.
/// </summary>
public class CreateContractTemplateVersionRequest
{
    public Guid ContractTypeId { get; set; }
    public int Version { get; set; }
    public string? TemplateFileUrl { get; set; }
    public string? ContentJson { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
}