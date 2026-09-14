using ContractManagement.Domain.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Contract;

public class ContractConfiguration : IEntityTypeConfiguration<ContractManagement.Domain.Contract.Entities.Contract>
{
    public void Configure(EntityTypeBuilder<ContractManagement.Domain.Contract.Entities.Contract> builder)
    {
        builder.ToTable("CONTRACTS");

        builder.HasKey(x => x.Id);

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
            .HasDefaultValue(0);

        builder.Property(x => x.SignedDate);

        builder.Property(x => x.EffectiveDate)
            .IsRequired();

        builder.Property(x => x.ExpiryDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue((byte)0);

        builder.Property(x => x.FileUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.ParentContractId);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ContractNumber)
            .IsUnique();

        builder.HasIndex(x => x.PartnerId);

        builder.HasIndex(x => x.OwnerId);

        builder.HasIndex(x => x.ContractTypeId);

        builder.HasIndex(x => x.TemplateVersionUsedId);

        builder.HasIndex(x => x.ParentContractId);

        builder.HasIndex(x => new { x.Status, x.ExpiryDate })
            .IncludeProperties(x => new { x.ContractNumber, x.Title, x.Value, x.OwnerId });

        builder.HasCheckConstraint("CK_CONTRACTS_Status", "([Status] >= 0 AND [Status] <= 7)");
        builder.HasCheckConstraint("CK_CONTRACTS_ExpiryAfterEffective", "([ExpiryDate] >= [EffectiveDate])");

        // Quan hệ N-1 với ContractTypes
        builder.HasOne(x => x.ContractType)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.ContractTypeId);

        // Quan hệ N-1 với ContractTemplateVersions
        builder.HasOne(x => x.TemplateVersionUsed)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.TemplateVersionUsedId);

        // Quan hệ tự tham chiếu N-1 với Contracts (ParentContract)
        builder.HasOne<ContractManagement.Domain.Contract.Entities.Contract>()
            .WithMany()
            .HasForeignKey("ParentContractId")
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete on self-reference
    }
}