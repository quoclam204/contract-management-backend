using ContractManagement.Domain.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Contracts;

public class ContractTypeConfiguration : IEntityTypeConfiguration<ContractType>
{
    public void Configure(EntityTypeBuilder<ContractType> builder)
    {
        builder.ToTable("CONTRACT_TYPES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Unique constraint on Name
        builder.HasIndex(x => x.Name)
            .IsUnique();

        // Relationship with ContractTemplateVersion (one-to-many)
        builder.HasMany(x => x.TemplateVersions)
            .WithOne(x => x.ContractType)
            .HasForeignKey(x => x.ContractTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Contract (one-to-many)
        builder.HasMany(x => x.Contracts)
            .WithOne(x => x.ContractType)
            .HasForeignKey(x => x.ContractTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
