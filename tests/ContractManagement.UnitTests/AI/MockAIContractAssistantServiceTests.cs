using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Services;
using Xunit;

namespace ContractManagement.UnitTests.AI;

public class MockAIContractAssistantServiceTests
{
    private readonly MockAIContractAssistantService _service = new();
    private readonly Guid _testContractId = Guid.NewGuid();

    #region Extract Tests

    [Fact]
    public async Task ExtractContractInfoAsync_WithValidContent_ExtractsAllFields()
    {
        // Arrange
        var content = @"Contract Number: HD-2026-001
Title: Service Delivery Agreement
Partner: ABC Technology Corp
Contract Type: Service
Value: 500000000
Signed Date: 2026-01-15
Effective Date: 2026-02-01
Expiry Date: 2027-02-01
Status: Active";

        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.ExtractContractInfoAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HD-2026-001", result.ContractNumber);
        Assert.Equal("Service Delivery Agreement", result.Title);
        Assert.Equal("ABC Technology Corp", result.Partner);
        Assert.Equal("Service", result.ContractType);
        Assert.Equal(500000000, result.Value);
        Assert.NotNull(result.SignedDate);
        Assert.NotNull(result.EffectiveDate);
        Assert.NotNull(result.ExpiryDate);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task ExtractContractInfoAsync_WithPartialContent_ReturnsPartialData()
    {
        // Arrange
        var content = "Contract Number: HD-2026-002\nTitle: Purchase Agreement";
        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.ExtractContractInfoAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HD-2026-002", result.ContractNumber);
        Assert.Equal("Purchase Agreement", result.Title);
        Assert.Null(result.Partner);
        Assert.Null(result.ContractType);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ExtractContractInfoAsync_WithEmptyContent_ReturnsEmptyFields()
    {
        // Arrange
        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = ""
        };

        // Act
        var result = await _service.ExtractContractInfoAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ContractNumber);
        Assert.Null(result.Title);
        Assert.Null(result.Partner);
    }

    [Fact]
    public async Task ExtractContractInfoAsync_WithNullContent_ReturnsEmptyFields()
    {
        // Arrange
        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = null
        };

        // Act
        var result = await _service.ExtractContractInfoAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ContractNumber);
    }

    [Fact]
    public async Task ExtractContractInfoAsync_WithInvalidContractId_ThrowsArgumentException()
    {
        // Arrange
        var request = new ExtractContractRequest
        {
            ContractId = Guid.Empty,
            ContractContent = "test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ExtractContractInfoAsync(request));
    }

    [Fact]
    public async Task ExtractContractInfoAsync_WithFormattedValue_ParsesCorrectly()
    {
        // Arrange
        var content = "Value: 1,500,000.00";
        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.ExtractContractInfoAsync(request);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal(1500000, result.Value);
    }

    [Fact]
    public async Task ExtractContractInfoAsync_WithVietnameseName_ExtractsCorrectly()
    {
        // Arrange
        var content = "Số HĐ: HD-VN-001\nTên HĐ: Hợp đồng cung cấp dịch vụ";
        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.ExtractContractInfoAsync(request);

        // Assert
        Assert.NotNull(result.ContractNumber);
        Assert.NotNull(result.Title);
    }

    #endregion

    #region Summary Tests

    [Fact]
    public async Task SummarizeContractAsync_WithValidContent_ReturnsSummary()
    {
        // Arrange
        var content = @"Service Delivery Agreement
This agreement outlines the terms for service delivery.
Services include technical support and maintenance.
Payment is due monthly.
Contract starts 2026-02-01 and expires 2027-02-01.";

        var request = new SummarizeContractRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.SummarizeContractAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testContractId, result.ContractId);
        Assert.NotEmpty(result.Summary);
        Assert.NotEmpty(result.KeyPoints);
        Assert.True(result.Summary.Length > 0);
        Assert.True(result.Summary.Length <= 503); // 500 + "..."
    }

    [Fact]
    public async Task SummarizeContractAsync_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var request = new SummarizeContractRequest
        {
            ContractId = _testContractId,
            ContractContent = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SummarizeContractAsync(request));
    }

    [Fact]
    public async Task SummarizeContractAsync_WithWhitespaceContent_ThrowsArgumentException()
    {
        // Arrange
        var request = new SummarizeContractRequest
        {
            ContractId = _testContractId,
            ContractContent = "   \n\n   "
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SummarizeContractAsync(request));
    }

    [Fact]
    public async Task SummarizeContractAsync_WithInvalidContractId_ThrowsArgumentException()
    {
        // Arrange
        var request = new SummarizeContractRequest
        {
            ContractId = Guid.Empty,
            ContractContent = "valid content"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SummarizeContractAsync(request));
    }

    [Fact]
    public async Task SummarizeContractAsync_ExtractsKeyPoints()
    {
        // Arrange
        var content = @"Main clause 1: Payment terms specified here.
Main clause 2: Termination conditions detailed.
Main clause 3: Liability limitations stated.";

        var request = new SummarizeContractRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.SummarizeContractAsync(request);

        // Assert
        Assert.NotEmpty(result.KeyPoints);
        Assert.True(result.KeyPoints.Count > 0);
    }

    [Fact]
    public async Task SummarizeContractAsync_WithLongContent_TruncatesSummary()
    {
        // Arrange
        var longContent = string.Join("\n", Enumerable.Range(1, 50)
            .Select(i => $"Line {i}: This is a very long contract content that needs to be summarized and may be truncated if it exceeds maximum length."));

        var request = new SummarizeContractRequest
        {
            ContractId = _testContractId,
            ContractContent = longContent
        };

        // Act
        var result = await _service.SummarizeContractAsync(request);

        // Assert
        Assert.NotEmpty(result.Summary);
        Assert.True(result.Summary.Length <= 503);
    }

    #endregion

    #region Risk Analysis Tests

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithExpiryKeyword_DetectsExpirytRisk()
    {
        // Arrange
        var content = "The contract expires on 2027-02-01. Renewal procedures must be initiated 60 days prior to expiry.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Risks);
        var expiryRisk = result.Risks.FirstOrDefault(r => r.Title.Contains("Expiration"));
        Assert.NotNull(expiryRisk);
        Assert.Equal(RiskLevel.Medium, expiryRisk.Level);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithPenaltyKeyword_DetectsHighRisk()
    {
        // Arrange
        var content = "Late payment penalties of 5% per month will be applied.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotEmpty(result.Risks);
        var penaltyRisk = result.Risks.FirstOrDefault(r => r.Title.Contains("Penalty"));
        Assert.NotNull(penaltyRisk);
        Assert.Equal(RiskLevel.High, penaltyRisk.Level);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithDisputeKeyword_DetectsHighRisk()
    {
        // Arrange
        var content = "Any disputes shall be resolved through arbitration in New York courts.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotEmpty(result.Risks);
        var disputeRisk = result.Risks.FirstOrDefault(r => r.Title.Contains("Dispute"));
        Assert.NotNull(disputeRisk);
        Assert.Equal(RiskLevel.High, disputeRisk.Level);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithConfidentialityKeyword_DetectsMediumRisk()
    {
        // Arrange
        var content = "All information disclosed is confidential and subject to NDA terms.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotEmpty(result.Risks);
        var confidentialRisk = result.Risks.FirstOrDefault(r => r.Title.Contains("Confidentiality"));
        Assert.NotNull(confidentialRisk);
        Assert.Equal(RiskLevel.Medium, confidentialRisk.Level);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithForceMajeureKeyword_DetectsLowRisk()
    {
        // Arrange
        var content = "Force majeure events are excluded from liability.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotEmpty(result.Risks);
        var fmRisk = result.Risks.FirstOrDefault(r => r.Title.Contains("Force Majeure"));
        Assert.NotNull(fmRisk);
        Assert.Equal(RiskLevel.Low, fmRisk.Level);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithNoRiskKeywords_ReturnsEmptyRisks()
    {
        // Arrange
        var content = "This is a simple contract with basic terms and conditions.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Risks);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithMultipleRisks_DetectsAll()
    {
        // Arrange
        var content = @"Contract expires on 2027-02-01.
Late payment penalties apply.
Disputes resolved through arbitration.
Confidential information protected.";

        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Risks.Count >= 3);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AnalyzeContractRiskAsync(request));
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithInvalidContractId_ThrowsArgumentException()
    {
        // Arrange
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = Guid.Empty,
            ContractContent = "valid content"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AnalyzeContractRiskAsync(request));
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_RiskLevelEnum_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (byte)RiskLevel.Low);
        Assert.Equal(1, (byte)RiskLevel.Medium);
        Assert.Equal(2, (byte)RiskLevel.High);
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithVietnameseRiskKeywords_DetectsRisks()
    {
        // Arrange
        var content = "Hợp đồng hết hạn vào ngày 2027-02-01. Phạt trễ hạn 5% mỗi tháng. Tranh chấp phải giải quyết tại tòa án.";
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = content
        };

        // Act
        var result = await _service.AnalyzeContractRiskAsync(request);

        // Assert
        Assert.NotEmpty(result.Risks);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task ExtractContractInfoAsync_WithCancelledToken_Throws()
    {
        // Arrange
        var request = new ExtractContractRequest
        {
            ContractId = _testContractId,
            ContractContent = "test"
        };
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Task.Delay throws TaskCanceledException, which is a subclass of OperationCanceledException
        await Assert.ThrowsAsync<System.Threading.Tasks.TaskCanceledException>(() =>
            _service.ExtractContractInfoAsync(request, cts.Token));
    }

    [Fact]
    public async Task SummarizeContractAsync_WithCancelledToken_Throws()
    {
        // Arrange
        var request = new SummarizeContractRequest
        {
            ContractId = _testContractId,
            ContractContent = "test content"
        };
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Task.Delay throws TaskCanceledException, which is a subclass of OperationCanceledException
        await Assert.ThrowsAsync<System.Threading.Tasks.TaskCanceledException>(() =>
            _service.SummarizeContractAsync(request, cts.Token));
    }

    [Fact]
    public async Task AnalyzeContractRiskAsync_WithCancelledToken_Throws()
    {
        // Arrange
        var request = new AnalyzeContractRiskRequest
        {
            ContractId = _testContractId,
            ContractContent = "test content"
        };
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Task.Delay throws TaskCanceledException, which is a subclass of OperationCanceledException
        await Assert.ThrowsAsync<System.Threading.Tasks.TaskCanceledException>(() =>
            _service.AnalyzeContractRiskAsync(request, cts.Token));
    }

    #endregion
}
