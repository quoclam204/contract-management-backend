using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Events;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Application.Workflow.Services;
using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Domain.Workflow.Enums;
using ContractManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContractManagement.UnitTests.Workflow;

public class ApprovalWorkflowExecutionTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    private class FakePublisher : IPublisher
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
    }

    private static (ApprovalService approvalService, WorkflowService workflowService, FakePublisher publisher, ContractManagementDbContext context) CreateSut()
    {
        var context = CreateInMemoryDbContext();
        var publisher = new FakePublisher();
        var evaluator = new WorkflowConditionEvaluator();
        var workflowService = new WorkflowService(context, evaluator);
        var approvalService = new ApprovalService(
            context,
            workflowService,
            publisher,
            NullLogger<ApprovalService>.Instance);

        return (approvalService, workflowService, publisher, context);
    }

    private static async Task<WorkflowDefinition> Seed3StepWorkflowAsync(ContractManagementDbContext context)
    {
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Quy trình duyệt 3 cấp",
            ConditionExpression = "Value >= 100000000",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        workflow.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflow.Id,
            StepOrder = 1,
            ApproverRole = ApproverRole.Staff,
            IsRequired = true
        });

        workflow.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflow.Id,
            StepOrder = 2,
            ApproverRole = ApproverRole.Manager,
            IsRequired = true
        });

        workflow.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflow.Id,
            StepOrder = 3,
            ApproverRole = ApproverRole.Admin,
            IsRequired = true
        });

        context.WorkflowDefinitions.Add(workflow);
        await context.SaveChangesAsync();

        return workflow;
    }

    #region 1. Sequential Approval Tests

    [Fact]
    public async Task SequentialApproval_Step1_ThenStep2_ThenStep3_PublishesWorkflowApprovedEvent()
    {
        // Arrange
        var (approvalService, _, publisher, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        // 1. Submit for approval
        var progress = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 200_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        Assert.Equal(3, progress.TotalSteps);
        Assert.Equal(1, progress.CurrentPendingStepOrder);
        Assert.Equal("Pending", progress.OverallStatus);
        Assert.Equal(0, progress.ApprovedStepsCount);

        var step1 = progress.Steps.First(s => s.StepOrder == 1);
        var step2 = progress.Steps.First(s => s.StepOrder == 2);
        var step3 = progress.Steps.First(s => s.StepOrder == 3);

        // 2. Approve Step 1
        var afterStep1 = await approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
        {
            ApprovalStepId = step1.Id,
            ApproverId = Guid.NewGuid(),
            Decision = ApprovalDecision.Approved,
            Comment = "Trưởng phòng duyệt OK"
        });

        Assert.Equal(1, afterStep1.ApprovedStepsCount);
        Assert.Equal(2, afterStep1.CurrentPendingStepOrder);
        Assert.Equal("Pending", afterStep1.OverallStatus);
        Assert.Empty(publisher.PublishedEvents.OfType<WorkflowApprovedEvent>());

        // 3. Approve Step 2
        var afterStep2 = await approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
        {
            ApprovalStepId = step2.Id,
            ApproverId = Guid.NewGuid(),
            Decision = ApprovalDecision.Approved,
            Comment = "Pháp chế thẩm định hợp lệ"
        });

        Assert.Equal(2, afterStep2.ApprovedStepsCount);
        Assert.Equal(3, afterStep2.CurrentPendingStepOrder);
        Assert.Equal("Pending", afterStep2.OverallStatus);
        Assert.Empty(publisher.PublishedEvents.OfType<WorkflowApprovedEvent>());

        // 4. Approve Step 3 (Final step)
        var afterStep3 = await approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
        {
            ApprovalStepId = step3.Id,
            ApproverId = Guid.NewGuid(),
            Decision = ApprovalDecision.Approved,
            Comment = "Tổng Giám đốc phê duyệt chính thức"
        });

        Assert.Equal(3, afterStep3.ApprovedStepsCount);
        Assert.Null(afterStep3.CurrentPendingStepOrder);
        Assert.Equal("Approved", afterStep3.OverallStatus);

        // Final approval MUST publish WorkflowApprovedEvent
        var approvedEvent = Assert.Single(publisher.PublishedEvents.OfType<WorkflowApprovedEvent>());
        Assert.Equal(contractId, approvedEvent.ContractId);
    }

    [Fact]
    public async Task ProcessDecision_WhenApprovingStep2BeforeStep1_ThrowsInvalidOperationException()
    {
        // Arrange
        var (approvalService, _, _, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        var progress = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 150_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        var step2 = progress.Steps.First(s => s.StepOrder == 2);

        // Act & Assert: Trying to approve step 2 while step 1 is still pending
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
            {
                ApprovalStepId = step2.Id,
                ApproverId = Guid.NewGuid(),
                Decision = ApprovalDecision.Approved
            }));

        Assert.Contains("Không thể duyệt bước 2 vì bước 1 chưa được phê duyệt", ex.Message);
    }

    [Fact]
    public async Task ProcessDecision_WhenApprovingStep3BeforeStep1And2_ThrowsInvalidOperationException()
    {
        // Arrange
        var (approvalService, _, _, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        var progress = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 150_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        var step3 = progress.Steps.First(s => s.StepOrder == 3);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
            {
                ApprovalStepId = step3.Id,
                ApproverId = Guid.NewGuid(),
                Decision = ApprovalDecision.Approved
            }));

        Assert.Contains("Không thể duyệt bước 3 vì bước 1 chưa được phê duyệt", ex.Message);
    }

    #endregion

    #region 2. Rejection Tests

    [Fact]
    public async Task ProcessDecision_WhenRejected_PublishesWorkflowRejectedEventWithReason()
    {
        // Arrange
        var (approvalService, _, publisher, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        var progress = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 150_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        var step1 = progress.Steps.First(s => s.StepOrder == 1);
        var rejectReason = "Hồ sơ đính kèm thiếu giấy phép kinh doanh của đối tác";

        // Act
        var result = await approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
        {
            ApprovalStepId = step1.Id,
            ApproverId = Guid.NewGuid(),
            Decision = ApprovalDecision.Rejected,
            Comment = rejectReason
        });

        // Assert
        Assert.Equal("Rejected", result.OverallStatus);
        Assert.Null(result.CurrentPendingStepOrder);

        var rejectedEvent = Assert.Single(publisher.PublishedEvents.OfType<WorkflowRejectedEvent>());
        Assert.Equal(contractId, rejectedEvent.ContractId);
        Assert.Equal(rejectReason, rejectedEvent.Reason);
    }

    [Fact]
    public async Task ProcessDecision_WhenRejectingWithoutReason_ThrowsArgumentException()
    {
        // Arrange
        var (approvalService, _, _, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        var progress = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 150_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        var step1 = progress.Steps.First(s => s.StepOrder == 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
            {
                ApprovalStepId = step1.Id,
                ApproverId = Guid.NewGuid(),
                Decision = ApprovalDecision.Rejected,
                Comment = "   " // Empty reason
            }));

        Assert.Contains("Vui lòng nhập lý do từ chối", ex.Message);
    }

    #endregion

    #region 3. Resubmission & In-Progress Constraint Tests

    [Fact]
    public async Task SubmitForApproval_WhenStepsAlreadyPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var (approvalService, _, _, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        // First submit succeeds
        await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 120_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        // Second submit while pending should fail
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
            {
                ContractId = contractId,
                ContractValue = 120_000_000m,
                WorkflowDefinitionId = workflow.Id
            }));

        Assert.Contains("Hợp đồng này đang trong tiến trình phê duyệt", ex.Message);
    }

    [Fact]
    public async Task Resubmission_AfterRejection_CleansUpOldStepsAndOpensNewCycle()
    {
        // Arrange
        var (approvalService, _, _, context) = CreateSut();
        var workflow = await Seed3StepWorkflowAsync(context);
        var contractId = Guid.NewGuid();

        // 1. First submission
        var progress1 = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 120_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        var step1 = progress1.Steps.First(s => s.StepOrder == 1);

        // 2. Reject at step 1
        await approvalService.ProcessDecisionAsync(new ProcessApprovalDecisionRequest
        {
            ApprovalStepId = step1.Id,
            ApproverId = Guid.NewGuid(),
            Decision = ApprovalDecision.Rejected,
            Comment = "Yêu cầu chỉnh sửa phụ lục"
        });

        // 3. Resubmission after rejection
        var progress2 = await approvalService.SubmitForApprovalAsync(new SubmitContractApprovalRequest
        {
            ContractId = contractId,
            ContractValue = 130_000_000m,
            WorkflowDefinitionId = workflow.Id
        });

        // Assert: New approval cycle has 3 pending steps, old rejected steps are cleared
        Assert.Equal(3, progress2.TotalSteps);
        Assert.Equal(1, progress2.CurrentPendingStepOrder);
        Assert.Equal("Pending", progress2.OverallStatus);
        Assert.Equal(0, progress2.ApprovedStepsCount);
        Assert.All(progress2.Steps, s => Assert.Equal(ApprovalDecision.Pending.ToString(), s.DecisionName));

        var dbSteps = await context.ApprovalSteps.Where(s => s.ContractId == contractId).ToListAsync();
        Assert.Equal(3, dbSteps.Count);
        Assert.All(dbSteps, s => Assert.Equal(ApprovalDecision.Pending, s.Decision));
    }

    #endregion
}
