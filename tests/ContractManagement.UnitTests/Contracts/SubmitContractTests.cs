using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Contracts.Handlers;
using ContractManagement.Application.Contracts.Services;
using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Events;
using ContractManagement.Application.Workflow.Handlers;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contracts.Enums;
using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContractManagement.UnitTests.Contracts;

public class SubmitContractTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    #region Test Doubles

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

    private class FakeWorkflowService : IWorkflowService
    {
        public ResolveWorkflowResponse ResolveResult { get; set; } = new();
        public decimal LastResolvedContractValue { get; private set; }

        public Task<ResolveWorkflowResponse> ResolveWorkflowForContractAsync(decimal contractValue)
        {
            LastResolvedContractValue = contractValue;
            return Task.FromResult(ResolveResult);
        }

        public Task<WorkflowDefinitionDto> CreateDefinitionAsync(CreateWorkflowDefinitionRequest request)
            => throw new NotImplementedException();

        public Task<WorkflowDefinitionDto?> GetDefinitionByIdAsync(Guid id)
            => throw new NotImplementedException();

        public Task<List<WorkflowDefinitionDto>> GetDefinitionsAsync(bool? isActive = null, string? searchKeyword = null)
            => throw new NotImplementedException();

        public Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CreateWorkflowVersionRequest request)
            => throw new NotImplementedException();

        public Task<WorkflowDefinitionDto?> UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest request)
            => throw new NotImplementedException();

        public Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive)
            => throw new NotImplementedException();

        public Task<bool> DeleteDefinitionAsync(Guid id)
            => throw new NotImplementedException();

        public EvaluateConditionResponse EvaluateCondition(string expression, decimal contractValue)
            => throw new NotImplementedException();
    }

    private class FakeApprovalService : IApprovalService
    {
        public SubmitContractApprovalRequest? LastSubmitRequest { get; private set; }

        public Task<ContractApprovalProgressDto> SubmitForApprovalAsync(SubmitContractApprovalRequest request)
        {
            LastSubmitRequest = request;
            return Task.FromResult(new ContractApprovalProgressDto
            {
                ContractId = request.ContractId,
                WorkflowDefinitionId = request.WorkflowDefinitionId ?? Guid.NewGuid(),
                WorkflowName = "Test Workflow",
                OverallStatus = "Pending"
            });
        }

        public Task<ContractApprovalProgressDto> ProcessDecisionAsync(ProcessApprovalDecisionRequest request)
            => throw new NotImplementedException();

        public Task<ContractApprovalProgressDto?> GetProgressByContractIdAsync(Guid contractId)
            => throw new NotImplementedException();

        public Task<List<PendingApprovalItemDto>> GetPendingApprovalsAsync(Guid? approverId = null)
            => throw new NotImplementedException();
    }

    #endregion

    [Fact]
    public async Task SubmitContractAsync_WhenDraft_TransitionsToPendingApprovalAndPublishesEvent()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        var workflowId = Guid.NewGuid();
        var templateVersion = new ContractTemplateVersion
        {
            Id = Guid.NewGuid(),
            ContractTypeId = Guid.NewGuid(),
            Version = 1,
            WorkflowDefinitionId = workflowId,
            CreatedBy = Guid.NewGuid()
        };
        context.ContractTemplateVersions.Add(templateVersion);

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = "HD-TEST-001",
            ContractTypeId = templateVersion.ContractTypeId,
            TemplateVersionUsedId = templateVersion.Id,
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Hợp đồng dịch vụ thử nghiệm",
            Value = 500_000_000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.Draft,
            RowVersion = new byte[] { 0, 0, 0, 1 }
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // Act
        await service.SubmitContractAsync(contract.Id);

        // Assert
        var savedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(savedContract);
        Assert.Equal(ContractStatus.PendingApproval, savedContract.Status);

        var submittedEvent = mediator.PublishedEvents.OfType<ContractSubmittedEvent>().FirstOrDefault();
        Assert.NotNull(submittedEvent);
        Assert.Equal(contract.Id, submittedEvent.ContractId);
        Assert.Equal(500_000_000m, submittedEvent.ContractValue);
        Assert.Equal(workflowId, submittedEvent.WorkflowDefinitionId);
    }

    [Fact]
    public async Task SubmitContractAsync_WhenContractNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SubmitContractAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SubmitContractAsync_WhenNotDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var service = new ContractService(context, mediator);

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = "HD-TEST-002",
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Hợp đồng đang chờ duyệt",
            Value = 100_000_000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.PendingApproval,
            RowVersion = new byte[] { 0, 0, 0, 1 }
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitContractAsync(contract.Id));
    }

    [Fact]
    public async Task ContractSubmittedEventHandler_WhenWorkflowIdProvided_UsesDirectWorkflowId()
    {
        // Arrange
        var workflowService = new FakeWorkflowService();
        var approvalService = new FakeApprovalService();
        var mediator = new FakeMediator();
        var handler = new ContractSubmittedEventHandler(
            workflowService,
            approvalService,
            mediator,
            NullLogger<ContractSubmittedEventHandler>.Instance);

        var contractId = Guid.NewGuid();
        var directWorkflowId = Guid.NewGuid();
        var notification = new ContractSubmittedEvent(contractId, 200_000_000m, directWorkflowId);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.NotNull(approvalService.LastSubmitRequest);
        Assert.Equal(contractId, approvalService.LastSubmitRequest.ContractId);
        Assert.Equal(directWorkflowId, approvalService.LastSubmitRequest.WorkflowDefinitionId);
        Assert.Equal(200_000_000m, approvalService.LastSubmitRequest.ContractValue);
    }

    [Fact]
    public async Task ContractSubmittedEventHandler_WhenNoWorkflowId_ResolvesByContractValue()
    {
        // Arrange
        var resolvedWorkflowId = Guid.NewGuid();
        var workflowService = new FakeWorkflowService
        {
            ResolveResult = new ResolveWorkflowResponse
            {
                IsMatched = true,
                Workflow = new WorkflowDefinitionDto
                {
                    Id = resolvedWorkflowId,
                    Name = "Luong duyet gia tri tren 500tr",
                    Version = 1,
                    IsActive = true
                }
            }
        };
        var approvalService = new FakeApprovalService();
        var mediator = new FakeMediator();
        var handler = new ContractSubmittedEventHandler(
            workflowService,
            approvalService,
            mediator,
            NullLogger<ContractSubmittedEventHandler>.Instance);

        var contractId = Guid.NewGuid();
        var notification = new ContractSubmittedEvent(contractId, 600_000_000m, null);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(600_000_000m, workflowService.LastResolvedContractValue);
        Assert.NotNull(approvalService.LastSubmitRequest);
        Assert.Equal(resolvedWorkflowId, approvalService.LastSubmitRequest.WorkflowDefinitionId);
        Assert.Equal(contractId, approvalService.LastSubmitRequest.ContractId);
    }

    [Fact]
    public async Task ContractSubmittedEventHandler_WhenResolveFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var workflowService = new FakeWorkflowService
        {
            ResolveResult = new ResolveWorkflowResponse
            {
                IsMatched = false,
                Message = "Khong tim thay luong duyet"
            }
        };
        var approvalService = new FakeApprovalService();
        var mediator = new FakeMediator();
        var handler = new ContractSubmittedEventHandler(
            workflowService,
            approvalService,
            mediator,
            NullLogger<ContractSubmittedEventHandler>.Instance);

        var notification = new ContractSubmittedEvent(Guid.NewGuid(), 10_000_000m, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task WorkflowApprovedEventHandler_TransitionsContractToApprovedAndPublishesEvent()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mediator = new FakeMediator();
        var handler = new WorkflowApprovedEventHandler(
            context,
            mediator,
            NullLogger<WorkflowApprovedEventHandler>.Instance);

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = "HD-APPROVE-001",
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Hợp đồng chờ phê duyệt hoàn tất",
            Value = 300_000_000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.PendingApproval,
            RowVersion = new byte[] { 0, 0, 0, 1 }
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // Act
        await handler.Handle(new WorkflowApprovedEvent(contract.Id), CancellationToken.None);

        // Assert
        var updatedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(ContractStatus.Approved, updatedContract.Status);

        var approvedEvent = mediator.PublishedEvents.OfType<ContractApprovedEvent>().FirstOrDefault();
        Assert.NotNull(approvedEvent);
        Assert.Equal(contract.Id, approvedEvent.ContractId);
    }

    [Fact]
    public async Task WorkflowRejectedEventHandler_TransitionsContractBackToDraft()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new WorkflowRejectedEventHandler(
            context,
            NullLogger<WorkflowRejectedEventHandler>.Instance);

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = "HD-REJECT-001",
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Hợp đồng bị từ chối duyệt",
            Value = 300_000_000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = ContractStatus.PendingApproval,
            RowVersion = new byte[] { 0, 0, 0, 1 }
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        // Act
        await handler.Handle(new WorkflowRejectedEvent(contract.Id, "Yêu cầu giảm giá trị thanh toán"), CancellationToken.None);

        // Assert
        var updatedContract = await context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(ContractStatus.Draft, updatedContract.Status);
    }
}
