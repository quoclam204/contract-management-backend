using ContractManagement.Domain.Contracts.Entities;
using ContractEntity = ContractManagement.Domain.Contracts.Entities.Contract;
using ContractManagement.Domain.Contracts.Enums;
using Xunit;

namespace ContractManagement.UnitTests.Contracts;

public class ContractStateMachineTests
{
    private static ContractEntity CreateValidDraftContract()
    {
        return new ContractEntity
        {
            Id = Guid.NewGuid(),
            ContractNumber = "HD-2026-001",
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Hợp đồng dịch vụ bảo trì phần mềm",
            Value = 150_000_000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.Draft
        };
    }

    [Fact]
    public void Submit_WhenDraftAndValid_TransitionsToPendingApproval()
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act
        contract.Submit();

        // Assert
        Assert.Equal(ContractStatus.PendingApproval, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Signed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expiring)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    public void Submit_WhenStatusNotDraft_ThrowsInvalidOperationException(ContractStatus nonDraftStatus)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = nonDraftStatus;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Submit());
        Assert.Contains("Draft", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Submit_WhenTitleIsNullOrEmpty_ThrowsInvalidOperationException(string? title)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Title = title!;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Submit());
        Assert.Contains("Tiêu đề", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Submit_WhenContractNumberIsNullOrEmpty_ThrowsInvalidOperationException(string? contractNumber)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.ContractNumber = contractNumber!;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Submit());
        Assert.Contains("Số hợp đồng", ex.Message);
    }

    [Fact]
    public void Submit_WhenExpiryDateBeforeEffectiveDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.EffectiveDate = DateTime.UtcNow;
        contract.ExpiryDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Submit());
        Assert.Contains("Ngày kết thúc hiệu lực", ex.Message);
    }

    [Fact]
    public void Approve_WhenPendingApproval_TransitionsToApproved()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Submit();

        // Act
        contract.Approve();

        // Assert
        Assert.Equal(ContractStatus.Approved, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Signed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expiring)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    public void Approve_WhenNotPendingApproval_ThrowsInvalidOperationException(ContractStatus invalidStatus)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = invalidStatus;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Approve());
        Assert.Contains("PendingApproval", ex.Message);
    }

    [Fact]
    public void Reject_WhenPendingApproval_TransitionsBackToDraft()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Submit();

        // Act
        contract.Reject("Ngân sách vượt định mức cho phép");

        // Assert
        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Signed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expiring)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    public void Reject_WhenNotPendingApproval_ThrowsInvalidOperationException(ContractStatus invalidStatus)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = invalidStatus;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Reject("Lý do từ chối"));
        Assert.Contains("PendingApproval", ex.Message);
    }

    [Fact]
    public void Sign_WhenApproved_TransitionsToSignedAndSetsSignedDate()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Submit();
        contract.Approve();
        var signDate = DateTime.UtcNow;

        // Act
        contract.Sign(signDate);

        // Assert
        Assert.Equal(ContractStatus.Signed, contract.Status);
        Assert.Equal(signDate, contract.SignedDate);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenSigned_TransitionsToActive()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Submit();
        contract.Approve();
        contract.Sign();

        // Act
        contract.Activate();

        // Assert
        Assert.Equal(ContractStatus.Active, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Fact]
    public void Expire_WhenActive_TransitionsToExpiring()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Submit();
        contract.Approve();
        contract.Sign();
        contract.Activate();

        // Act
        contract.Expire();

        // Assert
        Assert.Equal(ContractStatus.Expiring, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Fact]
    public void Renew_WhenExpiring_TransitionsToRenewed()
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Submit();
        contract.Approve();
        contract.Sign();
        contract.Activate();
        contract.Expire();

        // Act
        contract.Renew();

        // Assert
        Assert.Equal(ContractStatus.Renewed, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expiring)]
    [InlineData(ContractStatus.Renewed)]
    public void Terminate_WhenActiveOrExpiringOrRenewed_TransitionsToTerminated(ContractStatus validStatus)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = validStatus;

        // Act
        contract.Terminate("Hai bên thỏa thuận chấm dứt");

        // Assert
        Assert.Equal(ContractStatus.Terminated, contract.Status);
        Assert.NotNull(contract.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    public void Terminate_WhenDraftOrPendingOrApproved_ThrowsInvalidOperationException(ContractStatus invalidStatus)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = invalidStatus;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => contract.Terminate("Chấm dứt sớm"));
    }

    [Theory]
    [InlineData(ContractStatus.Draft, ContractStatus.PendingApproval, true)]
    [InlineData(ContractStatus.Draft, ContractStatus.Approved, false)]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.Approved, true)]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.Draft, true)]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.Signed, false)]
    [InlineData(ContractStatus.Approved, ContractStatus.Signed, true)]
    [InlineData(ContractStatus.Approved, ContractStatus.Active, false)]
    [InlineData(ContractStatus.Signed, ContractStatus.Active, true)]
    [InlineData(ContractStatus.Active, ContractStatus.Expiring, true)]
    [InlineData(ContractStatus.Active, ContractStatus.Terminated, true)]
    [InlineData(ContractStatus.Expiring, ContractStatus.Renewed, true)]
    [InlineData(ContractStatus.Expiring, ContractStatus.Terminated, true)]
    [InlineData(ContractStatus.Renewed, ContractStatus.Terminated, true)]
    [InlineData(ContractStatus.Terminated, ContractStatus.Active, false)]
    [InlineData(ContractStatus.Terminated, ContractStatus.Draft, false)]
    public void CanTransitionTo_ReturnsExpectedResult(ContractStatus from, ContractStatus to, bool expected)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = from;

        // Act
        var canTransition = contract.CanTransitionTo(to);

        // Assert
        Assert.Equal(expected, canTransition);
    }
}
