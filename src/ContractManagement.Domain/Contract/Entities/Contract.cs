namespace ContractManagement.Domain.Contract.Entities;

/// <summary>
/// Bảng CONTRACTS: Lưu trữ thông tin hợp đồng
/// </summary>
public class Contract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ContractNumber { get; set; } = string.Empty;
    public Guid ContractTypeId { get; set; }
    public Guid TemplateVersionUsedId { get; set; }
    public Guid PartnerId { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; } = 0;
    public DateTime? SignedDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public byte Status { get; set; } = 0; // 0=Draft, 1=PendingApproval, 2=Approved, 3=Signed, 4=Active, 5=Expiring, 6=Renewed, 7=Terminated
    public string? FileUrl { get; set; }
    public Guid? ParentContractId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public virtual ContractType ContractType { get; set; } = null!;
    public virtual ContractTemplateVersion TemplateVersionUsed { get; set; } = null!;
}
