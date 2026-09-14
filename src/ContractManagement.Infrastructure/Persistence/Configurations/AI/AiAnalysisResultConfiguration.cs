using ContractManagement.Domain.AI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractManagement.Infrastructure.Persistence.Configurations.AI;

public class AiAnalysisResultConfiguration : IEntityTypeConfiguration<AiAnalysisResult>
{
    public void Configure(EntityTypeBuilder<AiAnalysisResult> builder)
    {
        builder.ToTable("AI_ANALYSIS_RESULTS", table =>
        {
            table.HasCheckConstraint("CK_AI_RiskFlags_IsJson", "([RiskFlags] IS NULL OR ISJSON([RiskFlags]) = 1)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.ContractId)
            .IsRequired();

        builder.Property(x => x.Summary);

        builder.Property(x => x.ExtractedValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.ExtractedExpiryDate);

        builder.Property(x => x.RiskFlags);

        builder.Property(x => x.AnalyzedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.ContractId);

        builder.HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
