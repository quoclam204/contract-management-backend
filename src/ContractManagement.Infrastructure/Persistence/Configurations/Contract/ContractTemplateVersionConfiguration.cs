using ContractManagement.Domain.Contract.Entities;
using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Contract;

public class ContractTemplateVersionConfiguration : IEntityTypeConfiguration<ContractTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ContractTemplateVersion> builder)
    {
        builder.ToTable("CONTRACT_TEMPLATE_VERSIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractTypeId)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.TemplateFileUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.ContentJson)
            .HasMaxLength(-1); // NVARCHAR(MAX)

        builder.Property(x => x.WorkflowDefinitionId);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Quan hệ N-1 với ContractTypes
        builder.HasOne(x => x.ContractType)
            .WithMany(x => x.ContractTemplateVersions)
            .HasForeignKey(x => x.ContractTypeId);

        // Quan hệ N-1 với WorkflowDefinitions (nullable)
        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany() // WorkflowDefinition doesn't have a direct inverse collection for this
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting workflow if used by template

        // Ràng buộc Unique (ContractTypeId, Version)
        builder.HasIndex(x => new { x.ContractTypeId, x.Version })
            .IsUnique();

        // Filtered unique index: Chỉ cho phép 1 bản IsActive = 1 cho mỗi ContractTypeId
        builder.HasIndex(x => x.ContractTypeId)
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
