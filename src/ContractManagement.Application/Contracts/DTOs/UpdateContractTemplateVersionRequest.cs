namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Request model for updating an existing contract template version.
/// </summary>
public class UpdateContractTemplateVersionRequest
{
    public string? TemplateFileUrl { get; set; }
    public string? ContentJson { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public bool? IsActive { get; set; }
}