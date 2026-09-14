using ContractManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Partners;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("PARTNERS");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnType("uniqueidentifier")
            .HasDefaultValueSql("NEWID()");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(300)
            .HasColumnType("nvarchar");

        builder.Property(p => p.TaxCode)
            .HasMaxLength(50)
            .HasColumnType("nvarchar");

        builder.Property(p => p.Representative)
            .HasMaxLength(200)
            .HasColumnType("nvarchar");

        builder.Property(p => p.ContactEmail)
            .HasMaxLength(256)
            .HasColumnType("nvarchar");

        builder.Property(p => p.Address)
            .HasMaxLength(500)
            .HasColumnType("nvarchar");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasColumnType("datetime2");

        // Indexes matching database.sql exactly
        builder.HasIndex(p => p.TaxCode)
            .HasFilter("[TaxCode] IS NOT NULL")
            .IsUnique(); // Matches UX_PARTNERS_TaxCode (filtered unique index)
    }
}