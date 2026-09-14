using ContractManagement.Domain.AI.Entities;

namespace ContractManagement.UnitTests.AI;

public class AiAnalysisResultTests
{
    [Fact]
    public void Constructor_WithValidContractId_SetsExpectedProperties()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var expiryDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = new AiAnalysisResult(
            contractId,
            "Contract summary",
            1500m,
            expiryDate,
            "[]");

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(contractId, result.ContractId);
        Assert.Equal("Contract summary", result.Summary);
        Assert.Equal(1500m, result.ExtractedValue);
        Assert.Equal(expiryDate, result.ExtractedExpiryDate);
        Assert.Equal("[]", result.RiskFlags);
        Assert.NotEqual(default, result.AnalyzedAt);
    }

    [Fact]
    public void Constructor_WithEmptyContractId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AiAnalysisResult(Guid.Empty));
    }

    [Fact]
    public void Constructor_WithNegativeExtractedValue_ThrowsArgumentException()
    {
        // Arrange
        var contractId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AiAnalysisResult(contractId, extractedValue: -1m));
    }
}
