using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Domain.Workflow.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WORKFLOW_DEFINITIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ConditionExpression)
            .HasMaxLength(500);

        builder.Property(x => x.Version)
            .HasDefaultValue(1);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Ràng buộc Unique (Name, Version)
        builder.HasIndex(x => new { x.Name, x.Version })
            .IsUnique();

        // Filtered unique index: Chỉ cho phép 1 bản IsActive = 1 trên mỗi Name
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        // Quan hệ 1-N với WorkflowSteps (Cascade delete)
        builder.HasMany(x => x.WorkflowSteps)
            .WithOne(x => x.WorkflowDefinition)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quan hệ 1-N với ApprovalSteps (Restrict delete để giữ lịch sử)
        builder.HasMany(x => x.ApprovalSteps)
            .WithOne(x => x.WorkflowDefinition)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WORKFLOW_STEPS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApproverRole)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.IsRequired)
            .HasDefaultValue(true);

        builder.Property(x => x.MinimumAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m);

        // Ràng buộc Unique (WorkflowDefinitionId, StepOrder)
        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.StepOrder })
            .IsUnique();

        builder.HasIndex(x => x.WorkflowDefinitionId);
    }
}

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("APPROVAL_STEPS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Decision)
            .HasConversion<byte>()
            .HasDefaultValue(ApprovalDecision.Pending);

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.ContractId);
        builder.HasIndex(x => x.ApproverId);
    }
}