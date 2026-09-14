namespace ContractManagement.Domain.Contracts.Entities;

/// <summary>
/// Bảng CONTRACT_TEMPLATE_VERSIONS: Phiên bản mẫu hợp đồng
/// </summary>
public class ContractTemplateVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContractTypeId { get; set; }

    public int Version { get; set; }

    public string? TemplateFileUrl { get; set; }

    public string? ContentJson { get; set; } // Note: EF Core will map to nvarchar(MAX)

    public Guid? WorkflowDefinitionId { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ContractType ContractType { get; set; } = null!;
}
