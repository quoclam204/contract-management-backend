using ContractManagement.Domain.Workflow.Entities;

namespace ContractManagement.Domain.Contract.Entities;

/// <summary>
/// Bảng CONTRACT_TEMPLATE_VERSIONS: Phiên bản mẫu hợp đồng
/// Mỗi phiên bản thuộc về một loại hợp đồng và có thể gắn với một workflow definition
/// </summary>
public class ContractTemplateVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractTypeId { get; set; }
    public int Version { get; set; }
    public string? TemplateFileUrl { get; set; }
    public string? ContentJson { get; set; } // Lưu trữ JSON cấu trúc trường/điều khoản của mẫu
    public Guid? WorkflowDefinitionId { get; set; } // FK tới WORKFLOW_DEFINITIONS
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ContractType ContractType { get; set; } = null!;
    public virtual WorkflowDefinition? WorkflowDefinition { get; set; }
    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
