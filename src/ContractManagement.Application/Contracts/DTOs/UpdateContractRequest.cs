namespace ContractManagement.Application.Contracts.DTOs;

/// <summary>
/// Request model for updating an existing contract.
/// </summary>
public class UpdateContractRequest
{
    public string? ContractNumber { get; set; }
    public Guid? ContractTypeId { get; set; }
    public Guid? TemplateVersionUsedId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Title { get; set; }
    public decimal? Value { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? FileUrl { get; set; }
    public Guid? ParentContractId { get; set; }
}
