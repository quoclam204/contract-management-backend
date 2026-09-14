using ContractManagement.Domain.Contracts.Enums;

namespace ContractManagement.Domain.Contracts.Entities;

/// <summary>
/// Bảng CONTRACTS: Hợp đồng
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

    public decimal Value { get; set; } = 0m;

    public DateTime? SignedDate { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public string? FileUrl { get; set; }

    public Guid? ParentContractId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public virtual ContractType ContractType { get; set; } = null!;
    public virtual ContractTemplateVersion TemplateVersionUsed { get; set; } = null!;
    public virtual Contract? ParentContract { get; set; }
    public virtual ICollection<Contract> ChildContracts { get; set; } = new List<Contract>();

    /// <summary>
    /// Kiểm tra xem có thể chuyển sang trạng thái mục tiêu hay không.
    /// </summary>
    public bool CanTransitionTo(ContractStatus targetStatus)
    {
        return (Status, targetStatus) switch
        {
            (ContractStatus.Draft, ContractStatus.PendingApproval) => true,
            (ContractStatus.PendingApproval, ContractStatus.Approved) => true,
            (ContractStatus.PendingApproval, ContractStatus.Draft) => true, // Rejected by workflow
            (ContractStatus.Approved, ContractStatus.Signed) => true,
            (ContractStatus.Signed, ContractStatus.Active) => true,
            (ContractStatus.Active, ContractStatus.Expiring) => true,
            (ContractStatus.Active, ContractStatus.Terminated) => true,
            (ContractStatus.Expiring, ContractStatus.Renewed) => true,
            (ContractStatus.Expiring, ContractStatus.Terminated) => true,
            (ContractStatus.Renewed, ContractStatus.Terminated) => true,
            _ => false
        };
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết hợp đồng. Chỉ cho phép khi hợp đồng ở trạng thái Nháp (Draft).
    /// </summary>
    public void Update(
        string? title = null,
        string? contractNumber = null,
        Guid? contractTypeId = null,
        Guid? templateVersionUsedId = null,
        Guid? partnerId = null,
        Guid? ownerId = null,
        decimal? value = null,
        DateTime? signedDate = null,
        DateTime? effectiveDate = null,
        DateTime? expiryDate = null,
        string? fileUrl = null,
        Guid? parentContractId = null)
    {
        if (Status != ContractStatus.Draft)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng ở trạng thái Nháp (Draft) mới được phép chỉnh sửa. Trạng thái hiện tại: {Status}.");
        }

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Tiêu đề hợp đồng không được để trống.", nameof(title));
            Title = title.Trim();
        }

        if (contractNumber != null)
        {
            if (string.IsNullOrWhiteSpace(contractNumber))
                throw new ArgumentException("Số hợp đồng không được để trống.", nameof(contractNumber));
            ContractNumber = contractNumber.Trim();
        }

        if (contractTypeId.HasValue)
        {
            if (contractTypeId.Value == Guid.Empty)
                throw new ArgumentException("Loại hợp đồng không hợp lệ.", nameof(contractTypeId));
            ContractTypeId = contractTypeId.Value;
        }

        if (templateVersionUsedId.HasValue)
        {
            if (templateVersionUsedId.Value == Guid.Empty)
                throw new ArgumentException("Mẫu hợp đồng không hợp lệ.", nameof(templateVersionUsedId));
            TemplateVersionUsedId = templateVersionUsedId.Value;
        }

        if (partnerId.HasValue)
        {
            if (partnerId.Value == Guid.Empty)
                throw new ArgumentException("Đối tác không hợp lệ.", nameof(partnerId));
            PartnerId = partnerId.Value;
        }

        if (ownerId.HasValue)
        {
            OwnerId = ownerId.Value;
        }

        if (value.HasValue)
        {
            if (value.Value < 0)
                throw new ArgumentException("Giá trị hợp đồng không thể âm.", nameof(value));
            Value = value.Value;
        }

        if (signedDate.HasValue)
        {
            SignedDate = signedDate.Value;
        }

        var newEffectiveDate = effectiveDate ?? EffectiveDate;
        var newExpiryDate = expiryDate ?? ExpiryDate;

        if (effectiveDate.HasValue || expiryDate.HasValue)
        {
            if (newExpiryDate < newEffectiveDate)
            {
                throw new ArgumentException("Ngày kết thúc hiệu lực phải sau hoặc bằng ngày bắt đầu hiệu lực.", nameof(expiryDate));
            }
            EffectiveDate = newEffectiveDate;
            ExpiryDate = newExpiryDate;
        }

        if (fileUrl != null)
        {
            FileUrl = fileUrl;
        }

        if (parentContractId.HasValue)
        {
            ParentContractId = parentContractId.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Đệ trình hợp đồng vào quy trình phê duyệt (Draft -> PendingApproval).
    /// </summary>
    public void Submit()
    {
        if (Status != ContractStatus.Draft)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng ở trạng thái Nháp (Draft) mới có thể đệ trình duyệt. Trạng thái hiện tại: {Status}.");
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("Tiêu đề hợp đồng không được để trống khi gửi duyệt.");
        }

        if (string.IsNullOrWhiteSpace(ContractNumber))
        {
            throw new InvalidOperationException("Số hợp đồng không được để trống khi gửi duyệt.");
        }

        if (ContractTypeId == Guid.Empty)
        {
            throw new InvalidOperationException("Loại hợp đồng không hợp lệ.");
        }

        if (TemplateVersionUsedId == Guid.Empty)
        {
            throw new InvalidOperationException("Mẫu hợp đồng không hợp lệ.");
        }

        if (PartnerId == Guid.Empty)
        {
            throw new InvalidOperationException("Đối tác không hợp lệ.");
        }

        if (ExpiryDate < EffectiveDate)
        {
            throw new InvalidOperationException("Ngày kết thúc hiệu lực phải sau hoặc bằng ngày bắt đầu hiệu lực.");
        }

        Status = ContractStatus.PendingApproval;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Phê duyệt hợp đồng khi quy trình workflow hoàn tất (PendingApproval -> Approved).
    /// </summary>
    public void Approve()
    {
        if (Status != ContractStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng đang chờ duyệt (PendingApproval) mới có thể duyệt. Trạng thái hiện tại: {Status}.");
        }

        Status = ContractStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Từ chối phê duyệt hợp đồng trong quy trình workflow (PendingApproval -> Draft).
    /// Trả về trạng thái Draft để tác giả có thể chỉnh sửa và gửi duyệt lại.
    /// </summary>
    public void Reject(string? reason = null)
    {
        if (Status != ContractStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng đang chờ duyệt (PendingApproval) mới có thể từ chối. Trạng thái hiện tại: {Status}.");
        }

        Status = ContractStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ký kết hợp đồng (Approved -> Signed).
    /// </summary>
    public void Sign(DateTime? signedDate = null)
    {
        if (Status != ContractStatus.Approved)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng đã duyệt (Approved) mới có thể ký kết. Trạng thái hiện tại: {Status}.");
        }

        Status = ContractStatus.Signed;
        SignedDate = signedDate ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Kích hoạt hiệu lực hợp đồng (Signed -> Active).
    /// </summary>
    public void Activate()
    {
        if (Status != ContractStatus.Signed)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng đã ký (Signed) mới có thể kích hoạt hiệu lực. Trạng thái hiện tại: {Status}.");
        }

        Status = ContractStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Đánh dấu hợp đồng sắp hết hạn (Active -> Expiring).
    /// </summary>
    public void Expire()
    {
        if (Status != ContractStatus.Active)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng đang có hiệu lực (Active) mới có thể chuyển sang sắp hết hạn. Trạng thái hiện tại: {Status}.");
        }

        Status = ContractStatus.Expiring;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gia hạn hợp đồng (Expiring -> Renewed).
    /// </summary>
    public void Renew()
    {
        if (Status != ContractStatus.Expiring)
        {
            throw new InvalidOperationException($"Chỉ hợp đồng sắp hết hạn (Expiring) mới có thể gia hạn. Trạng thái hiện tại: {Status}.");
        }

        Status = ContractStatus.Renewed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Chấm dứt / thanh lý hợp đồng (Active / Expiring / Renewed -> Terminated).
    /// </summary>
    public void Terminate(string? reason = null)
    {
        if (Status != ContractStatus.Active && Status != ContractStatus.Expiring && Status != ContractStatus.Renewed)
        {
            throw new InvalidOperationException($"Không thể chấm dứt hợp đồng ở trạng thái {Status}. Chỉ hợp đồng Active, Expiring hoặc Renewed mới có thể chấm dứt.");
        }

        Status = ContractStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }
}
