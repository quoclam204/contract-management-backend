using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Contracts;

public class ContractConfiguration : IEntityTypeConfiguration<ContractManagement.Domain.Contracts.Entities.Contract>
{
    public void Configure(EntityTypeBuilder<ContractManagement.Domain.Contracts.Entities.Contract> builder)
    {
        builder.ToTable("CONTRACTS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.ContractNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ContractTypeId)
            .IsRequired();

        builder.Property(x => x.TemplateVersionUsedId)
            .IsRequired();

        builder.Property(x => x.PartnerId)
            .IsRequired();

        builder.Property(x => x.OwnerId)
            .IsRequired();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Value)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.SignedDate);

        builder.Property(x => x.EffectiveDate)
            .IsRequired();

        builder.Property(x => x.ExpiryDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(ContractStatus.Draft);

        builder.Property(x => x.FileUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.ParentContractId);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Unique constraint on ContractNumber
        builder.HasIndex(x => x.ContractNumber)
            .IsUnique();

        // Check constraint for Status between 0 and 7
        builder.HasCheckConstraint("CK_CONTRACTS_Status", "([Status] >= 0 AND [Status] <= 7)");

        // Check constraint for ExpiryDate >= EffectiveDate
        builder.HasCheckConstraint("CK_CONTRACTS_ExpiryAfterEffective", "([ExpiryDate] >= [EffectiveDate])");

        // Foreign key to ContractType
        builder.HasOne(x => x.ContractType)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.ContractTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to ContractTemplateVersion
        builder.HasOne(x => x.TemplateVersionUsed)
            .WithMany()
            .HasForeignKey(x => x.TemplateVersionUsedId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing foreign key for ParentContract
        builder.HasOne(x => x.ParentContract)
            .WithMany(x => x.ChildContracts)
            .HasForeignKey(x => x.ParentContractId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.PartnerId);
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.ContractTypeId);
        builder.HasIndex(x => x.TemplateVersionUsedId);
        builder.HasIndex(x => x.ParentContractId);

        builder.HasIndex(x => new { x.Status, x.ExpiryDate })
            .IncludeProperties(x => new { x.ContractNumber, x.Title, x.Value, x.OwnerId });
    }
}
