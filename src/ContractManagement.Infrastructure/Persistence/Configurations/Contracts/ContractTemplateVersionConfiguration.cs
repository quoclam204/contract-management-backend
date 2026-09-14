using ContractManagement.Domain.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Contracts;

public class ContractTemplateVersionConfiguration : IEntityTypeConfiguration<ContractTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ContractTemplateVersion> builder)
    {
        builder.ToTable("CONTRACT_TEMPLATE_VERSIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.ContractTypeId)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.TemplateFileUrl)
            .HasMaxLength(1000);


        builder.Property(x => x.WorkflowDefinitionId);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Foreign key to ContractType
        builder.HasOne(x => x.ContractType)
            .WithMany(x => x.TemplateVersions)
            .HasForeignKey(x => x.ContractTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on ContractTypeId and Version
        builder.HasIndex(x => new { x.ContractTypeId, x.Version })
            .IsUnique();

        // Check constraint for ContentJson being valid JSON
        builder.HasCheckConstraint("CK_CTV_ContentJson_IsJson", "JSON_VALID([ContentJson]) = 1");

        // Filtered unique index: Only one active version per contract type
        builder.HasIndex(x => new { x.ContractTypeId, x.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}