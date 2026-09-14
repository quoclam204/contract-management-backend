using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Domain.Workflow.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Workflow;

public class SignatureConfiguration : IEntityTypeConfiguration<Signature>
{
    public void Configure(EntityTypeBuilder<Signature> builder)
    {
        builder.ToTable("SIGNATURES", t =>
        {
            t.HasCheckConstraint("CK_SIGNATURES_Method", "[SignatureMethod] BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_SIGNATURES_SignerType", "[SignerType] BETWEEN 0 AND 1");
            t.HasCheckConstraint("CK_SIGNATURES_SignerExclusive", 
                "([SignerType] = 0 AND [InternalSignerId] IS NOT NULL AND [PartnerSignerId] IS NULL) OR " +
                "([SignerType] = 1 AND [PartnerSignerId] IS NOT NULL AND [InternalSignerId] IS NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractId)
            .IsRequired();

        builder.Property(x => x.SignerType)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.InternalSignerId)
            .IsRequired(false);

        builder.Property(x => x.PartnerSignerId)
            .IsRequired(false);

        builder.Property(x => x.SignerNameSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SignatureMethod)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.SignedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.SignatureHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(x => x.ContractId);
    }
}
