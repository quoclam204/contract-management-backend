namespace ContractManagement.Domain.Contracts.Entities;

/// <summary>
/// Bảng CONTRACT_TYPES: Loại hợp đồng
/// </summary>
public class ContractType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ContractTemplateVersion> TemplateVersions { get; set; } = new List<ContractTemplateVersion>();
    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
