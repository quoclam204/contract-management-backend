using ContractManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Attachments;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("ATTACHMENTS");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnType("uniqueidentifier")
            .HasDefaultValueSql("NEWID()");

        builder.Property(a => a.ContractId)
            .IsRequired()
            .HasColumnType("uniqueidentifier");

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("nvarchar");

        builder.Property(a => a.Version)
            .IsRequired()
            .HasDefaultValue(1)
            .HasColumnType("int");

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnType("nvarchar");

        builder.Property(a => a.UploadedBy)
            .IsRequired()
            .HasColumnType("uniqueidentifier");

        builder.Property(a => a.UploadedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasColumnType("datetime2");

        // Indexes matching database.sql exactly
        builder.HasIndex(a => a.ContractId);

        builder.HasIndex(a => new { a.ContractId, a.FileName, a.Version })
            .IsUnique();

        // Foreign keys matching database.sql
        builder.HasOne<ContractManagement.Domain.Contract.Entities.Contract>()
            .WithMany()
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ContractManagement.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
