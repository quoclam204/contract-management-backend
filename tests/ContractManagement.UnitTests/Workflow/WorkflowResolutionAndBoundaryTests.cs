using ContractManagement.Application.Workflow.Services;
using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Domain.Workflow.Enums;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContractManagement.UnitTests.Workflow;

public class WorkflowResolutionAndBoundaryTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    private static (WorkflowService service, ContractManagementDbContext context) CreateSut()
    {
        var context = CreateInMemoryDbContext();
        var evaluator = new WorkflowConditionEvaluator();
        var service = new WorkflowService(context, evaluator);
        return (service, context);
    }

    private static async Task SeedWorkflowsAsync(ContractManagementDbContext context)
    {
        // 1. Luồng giá trị nhỏ: < 100M
        var lowWf = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Luồng duyệt giá trị nhỏ (< 100 triệu)",
            ConditionExpression = "Value < 100000000",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        lowWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = lowWf.Id,
            StepOrder = 1,
            ApproverRole = ApproverRole.Staff,
            IsRequired = true
        });

        // 2. Luồng giá trị trung bình: 100M <= Value < 500M
        var medWf = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Luồng duyệt giá trị trung bình (100 triệu - 500 triệu)",
            ConditionExpression = "Value >= 100000000 AND Value < 500000000",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        medWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = medWf.Id,
            StepOrder = 1,
            ApproverRole = ApproverRole.Staff,
            IsRequired = true
        });
        medWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = medWf.Id,
            StepOrder = 2,
            ApproverRole = ApproverRole.Manager,
            IsRequired = true
        });

        // 3. Luồng giá trị lớn: >= 500M
        var highWf = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Luồng duyệt giá trị lớn (>= 500 triệu)",
            ConditionExpression = "Value >= 500000000",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        highWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = highWf.Id,
            StepOrder = 1,
            ApproverRole = ApproverRole.Staff,
            IsRequired = true
        });
        highWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = highWf.Id,
            StepOrder = 2,
            ApproverRole = ApproverRole.Manager,
            IsRequired = true
        });
        highWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = highWf.Id,
            StepOrder = 3,
            ApproverRole = ApproverRole.Admin,
            IsRequired = true
        });

        // 4. Luồng mặc định không có điều kiện
        var defaultWf = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Luồng duyệt mặc định",
            ConditionExpression = null,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        defaultWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = defaultWf.Id,
            StepOrder = 1,
            ApproverRole = ApproverRole.Approver,
            IsRequired = true
        });

        context.WorkflowDefinitions.AddRange(lowWf, medWf, highWf, defaultWf);
        await context.SaveChangesAsync();
    }

    #region 1. Value Condition & Boundary Value Tests

    [Theory]
    [InlineData(0, "Luồng duyệt giá trị nhỏ (< 100 triệu)")]
    [InlineData(50_000_000, "Luồng duyệt giá trị nhỏ (< 100 triệu)")]
    [InlineData(99_999_999, "Luồng duyệt giá trị nhỏ (< 100 triệu)")]
    [InlineData(100_000_000, "Luồng duyệt giá trị trung bình (100 triệu - 500 triệu)")]
    [InlineData(250_000_000, "Luồng duyệt giá trị trung bình (100 triệu - 500 triệu)")]
    [InlineData(499_999_999, "Luồng duyệt giá trị trung bình (100 triệu - 500 triệu)")]
    [InlineData(500_000_000, "Luồng duyệt giá trị lớn (>= 500 triệu)")]
    [InlineData(1_000_000_000, "Luồng duyệt giá trị lớn (>= 500 triệu)")]
    public async Task ResolveWorkflow_BoundaryValues_SelectsExpectedWorkflow(decimal value, string expectedWorkflowName)
    {
        // Arrange
        var (service, context) = CreateSut();
        await SeedWorkflowsAsync(context);

        // Act
        var result = await service.ResolveWorkflowForContractAsync(value);

        // Assert
        Assert.True(result.IsMatched);
        Assert.NotNull(result.Workflow);
        Assert.Equal(expectedWorkflowName, result.Workflow.Name);
    }

    [Fact]
    public async Task ResolveWorkflow_WhenConditionalWorkflowsDoNotMatch_FallsBackToDefaultWorkflow()
    {
        // Arrange
        var (service, context) = CreateSut();

        // Only default workflow active
        var defaultWf = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Luồng dự phòng chung",
            ConditionExpression = null,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        defaultWf.WorkflowSteps.Add(new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = defaultWf.Id,
            StepOrder = 1,
            ApproverRole = ApproverRole.Approver,
            IsRequired = true
        });
        context.WorkflowDefinitions.Add(defaultWf);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResolveWorkflowForContractAsync(75_000_000m);

        // Assert
        Assert.True(result.IsMatched);
        Assert.NotNull(result.Workflow);
        Assert.Equal("Luồng dự phòng chung", result.Workflow.Name);
    }

    [Fact]
    public async Task ResolveWorkflow_WhenNoActiveWorkflowsExist_ReturnsUnmatched()
    {
        // Arrange
        var (service, context) = CreateSut();

        var inactiveWf = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Luồng cũ",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        context.WorkflowDefinitions.Add(inactiveWf);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResolveWorkflowForContractAsync(100_000_000m);

        // Assert
        Assert.False(result.IsMatched);
        Assert.Null(result.Workflow);
        Assert.Contains("chưa cấu hình bất kỳ luồng duyệt nào đang hoạt động", result.Message);
    }

    #endregion
}
