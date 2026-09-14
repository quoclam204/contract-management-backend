using ContractManagement.Application.Contracts.DTOs;
using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Contracts.Handlers;
using ContractManagement.Application.Contracts.Services;
using ContractManagement.Application.Workflow.Events;
using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contracts.Enums;
using ContractManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContractManagement.UnitTests.Contracts;

public class ContractDetailAndUpdateTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    private class FakeMediator : IMediator
    {
        public List<INotification> PublishedEvents { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is INotification n)
                PublishedEvents.Add(n);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            PublishedEvents.Add(notification);
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotImplementedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private static Contract CreateValidDraftContract(Guid? id = null)
    {
        return new Contract
        {
            Id = id ?? Guid.NewGuid(),
            ContractNumber = "HD-TEST-001",
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Hợp đồng kiểm thử",
            Value = 50_000_000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.Draft,
            FileUrl = "https://example.com/contract.pdf",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = null
        };
    }

    #region 1. Get Contract Detail Tests

    [Fact]
    public async Task GetContractById_WhenContractExists_ReturnsFullContractDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        var contractType = new ContractType
        {
            Id = Guid.NewGuid(),
            Name = "Hợp đồng Dịch vụ",
            CreatedAt = DateTime.UtcNow
        };
        context.ContractTypes.Add(contractType);

        var contract = CreateValidDraftContract();
        contract.ContractTypeId = contractType.Id;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContractByIdAsync(contract.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contract.Id, result.Id);
        Assert.Equal(contract.ContractNumber, result.ContractNumber);
        Assert.Equal(contract.ContractTypeId, result.ContractTypeId);
        Assert.Equal(contractType.Name, result.ContractTypeName);
        Assert.Equal(contract.TemplateVersionUsedId, result.TemplateVersionUsedId);
        Assert.Equal(contract.PartnerId, result.PartnerId);
        Assert.Equal(contract.OwnerId, result.OwnerId);
        Assert.Equal(contract.Title, result.Title);
        Assert.Equal(contract.Value, result.Value);
        Assert.Equal(contract.EffectiveDate, result.EffectiveDate);
        Assert.Equal(contract.ExpiryDate, result.ExpiryDate);
        Assert.Equal(ContractStatus.Draft, result.Status);
        Assert.Equal(contract.FileUrl, result.FileUrl);
        Assert.Equal(contract.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task GetContractById_WhenContractDoesNotExist_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        // Act
        var result = await service.GetContractByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 2. Update Contract Tests (Draft Only & Validations)

    [Fact]
    public async Task Update_WhenDraft_UpdatesFieldsSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        var contractType = new ContractType { Id = Guid.NewGuid(), Name = "Hợp đồng Mua bán", CreatedAt = DateTime.UtcNow };
        context.ContractTypes.Add(contractType);

        var contract = CreateValidDraftContract();
        contract.ContractTypeId = contractType.Id;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var newEffective = DateTime.UtcNow.AddDays(1);
        var newExpiry = DateTime.UtcNow.AddYears(2);
        var newPartnerId = Guid.NewGuid();

        var updateRequest = new UpdateContractRequest
        {
            Title = "Tiêu đề mới đã chỉnh sửa",
            ContractNumber = "HD-NEW-999",
            ContractTypeId = contractType.Id,
            PartnerId = newPartnerId,
            Value = 120_000_000m,
            EffectiveDate = newEffective,
            ExpiryDate = newExpiry,
            FileUrl = "https://example.com/updated.pdf"
        };

        // Act
        var result = await service.UpdateContractAsync(contract.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tiêu đề mới đã chỉnh sửa", result.Title);
        Assert.Equal("HD-NEW-999", result.ContractNumber);
        Assert.Equal(contractType.Id, result.ContractTypeId);
        Assert.Equal("Hợp đồng Mua bán", result.ContractTypeName);
        Assert.Equal(newPartnerId, result.PartnerId);
        Assert.Equal(120_000_000m, result.Value);
        Assert.Equal(newEffective, result.EffectiveDate);
        Assert.Equal(newExpiry, result.ExpiryDate);
        Assert.Equal("https://example.com/updated.pdf", result.FileUrl);
        Assert.Equal(ContractStatus.Draft, result.Status); // Status stays Draft!
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task Update_WhenContractNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        var updateRequest = new UpdateContractRequest { Title = "Tiêu đề mới" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateContractAsync(Guid.NewGuid(), updateRequest));
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Signed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expiring)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    public void Update_WhenNonDraftStatus_ThrowsInvalidOperationException(ContractStatus nonDraftStatus)
    {
        // Arrange
        var contract = CreateValidDraftContract();
        contract.Status = nonDraftStatus;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => contract.Update(title: "Tiêu đề mới"));
        Assert.Contains("Chỉ hợp đồng ở trạng thái Nháp (Draft) mới được phép chỉnh sửa", ex.Message);
        Assert.Contains(nonDraftStatus.ToString(), ex.Message);
    }

    [Fact]
    public async Task UpdateContractAsync_WhenPendingApproval_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        var contract = CreateValidDraftContract();
        contract.Status = ContractStatus.PendingApproval;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateContractRequest { Title = "Tiêu đề chỉnh sửa" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateContractAsync(contract.Id, updateRequest));
        Assert.Contains("Chỉ hợp đồng ở trạng thái Nháp (Draft) mới được phép chỉnh sửa", ex.Message);
    }

    [Fact]
    public void Update_WithInvalidDates_ThrowsArgumentException()
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert: ExpiryDate earlier than EffectiveDate
        var ex = Assert.Throws<ArgumentException>(() =>
            contract.Update(
                effectiveDate: DateTime.UtcNow.AddDays(10),
                expiryDate: DateTime.UtcNow.AddDays(5)
            ));

        Assert.Contains("Ngày kết thúc hiệu lực phải sau hoặc bằng ngày bắt đầu hiệu lực", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyTitle_ThrowsArgumentException(string emptyTitle)
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => contract.Update(title: emptyTitle));
        Assert.Contains("Tiêu đề hợp đồng không được để trống", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyContractNumber_ThrowsArgumentException(string emptyContractNumber)
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => contract.Update(contractNumber: emptyContractNumber));
        Assert.Contains("Số hợp đồng không được để trống", ex.Message);
    }

    [Fact]
    public void Update_WithEmptyContractTypeId_ThrowsArgumentException()
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => contract.Update(contractTypeId: Guid.Empty));
        Assert.Contains("Loại hợp đồng không hợp lệ", ex.Message);
    }

    [Fact]
    public void Update_WithEmptyTemplateVersionUsedId_ThrowsArgumentException()
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => contract.Update(templateVersionUsedId: Guid.Empty));
        Assert.Contains("Mẫu hợp đồng không hợp lệ", ex.Message);
    }

    [Fact]
    public void Update_WithEmptyPartnerId_ThrowsArgumentException()
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => contract.Update(partnerId: Guid.Empty));
        Assert.Contains("Đối tác không hợp lệ", ex.Message);
    }

    [Fact]
    public void Update_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var contract = CreateValidDraftContract();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => contract.Update(value: -100m));
        Assert.Contains("Giá trị hợp đồng không thể âm", ex.Message);
    }

    #endregion

    #region 3. Full Lifecycle Tests: Reject -> Draft -> Edit -> Resubmit

    [Fact]
    public async Task WorkflowRejected_TransitionsContract_FromPendingApprovalToDraft()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new WorkflowRejectedEventHandler(context, NullLogger<WorkflowRejectedEventHandler>.Instance);

        var contract = CreateValidDraftContract();
        contract.Status = ContractStatus.PendingApproval; // Currently pending
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var rejectedEvent = new WorkflowRejectedEvent(contract.Id, "Hồ sơ chưa đủ chữ ký đối tác");

        // Act
        await handler.Handle(rejectedEvent, CancellationToken.None);

        // Assert
        var updatedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(ContractStatus.Draft, updatedContract.Status);
        Assert.NotNull(updatedContract.UpdatedAt);
    }

    [Fact]
    public async Task FullLifecycle_Reject_ThenEdit_ThenResubmit_Succeeds()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var contractService = new ContractService(context, mediator);
        var rejectHandler = new WorkflowRejectedEventHandler(context, NullLogger<WorkflowRejectedEventHandler>.Instance);

        var contractType = new ContractType { Id = Guid.NewGuid(), Name = "Hợp đồng Hợp tác", CreatedAt = DateTime.UtcNow };
        context.ContractTypes.Add(contractType);

        var contract = CreateValidDraftContract();
        contract.ContractTypeId = contractType.Id;
        contract.Status = ContractStatus.Draft;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // 1. Submit initial draft
        await contractService.SubmitContractAsync(contract.Id);
        var submittedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.PendingApproval, submittedContract!.Status);
        Assert.Single(mediator.PublishedEvents.OfType<ContractSubmittedEvent>());

        // 2. Workflow rejects the contract
        var rejectReason = "Cần điều chỉnh lại thời hạn hợp đồng";
        await rejectHandler.Handle(new WorkflowRejectedEvent(contract.Id, rejectReason), CancellationToken.None);
        var rejectedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.Draft, rejectedContract!.Status);

        // 3. Edit contract while in Draft
        var updateRequest = new UpdateContractRequest
        {
            Title = "Hợp đồng kiểm thử đã sửa đổi theo góp ý",
            ExpiryDate = DateTime.UtcNow.AddYears(3),
            Value = 75_000_000m
        };
        var updatedResult = await contractService.UpdateContractAsync(contract.Id, updateRequest);
        Assert.Equal("Hợp đồng kiểm thử đã sửa đổi theo góp ý", updatedResult.Title);
        Assert.Equal(75_000_000m, updatedResult.Value);
        Assert.Equal(ContractStatus.Draft, updatedResult.Status);

        // 4. Resubmit contract
        await contractService.SubmitContractAsync(contract.Id);
        var resubmittedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.Equal(ContractStatus.PendingApproval, resubmittedContract!.Status);

        // Verify a second ContractSubmittedEvent was published
        var submittedEvents = mediator.PublishedEvents.OfType<ContractSubmittedEvent>().ToList();
        Assert.Equal(2, submittedEvents.Count);
        Assert.Equal(75_000_000m, submittedEvents[1].ContractValue);
    }

    #endregion
}
