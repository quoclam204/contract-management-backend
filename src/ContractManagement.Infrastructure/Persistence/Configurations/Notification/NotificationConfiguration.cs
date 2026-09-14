using DomainNotification = ContractManagement.Domain.Notification.Entities;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.Notification;

/// <summary>
/// EF Core configuration for Notification entity
/// Maps exactly to database.sql NOTIFICATIONS table
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<DomainNotification.Notification>
{
    public void Configure(EntityTypeBuilder<DomainNotification.Notification> builder)
    {
        builder.ToTable("NOTIFICATIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uniqueidentifier")
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasColumnType("uniqueidentifier");

        builder.Property(x => x.ContractId)
            .HasColumnType("uniqueidentifier");

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<byte>()
            .HasColumnType("tinyint");

        builder.Property(x => x.IsRead)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnType("bit");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasColumnType("datetime2");

        // Check constraint: Type BETWEEN 0 AND 3
        builder.HasCheckConstraint("CK_NOTIFICATIONS_Type", "([Type] >= 0 AND [Type] <= 3)");

        // Index: (UserId, IsRead, CreatedAt DESC)
        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_NOTIFICATIONS_UserId_IsRead");
    }
}