using ContractManagement.Domain.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Contract;

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
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Quan hệ 1-N với ContractTemplateVersions
        builder.HasMany(x => x.ContractTemplateVersions)
            .WithOne(x => x.ContractType)
            .HasForeignKey(x => x.ContractTypeId);

        // Quan hệ 1-N với Contracts
        builder.HasMany(x => x.Contracts)
            .WithOne(x => x.ContractType)
            .HasForeignKey(x => x.ContractTypeId);
    }
}
