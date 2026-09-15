using ContractManagement.Domain.AI.Entities;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.UnitTests.AI;

public class AiAnalysisResultConfigurationTests
{
    [Fact]
    public void AiAnalysisResult_ModelConfiguration_MatchesDatabaseSchema()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ContractManagementDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(AiAnalysisResult));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("AI_ANALYSIS_RESULTS", entityType.GetTableName());
        Assert.NotNull(entityType.FindPrimaryKey());
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.Id)));
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.ContractId)));
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.Summary)));
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.ExtractedValue)));
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.ExtractedExpiryDate)));
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.RiskFlags)));
        Assert.NotNull(entityType.FindProperty(nameof(AiAnalysisResult.AnalyzedAt)));
        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Any(property => property.Name == nameof(AiAnalysisResult.ContractId)));
        Assert.Contains(entityType.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Any(property => property.Name == nameof(AiAnalysisResult.ContractId)));
    }
}
