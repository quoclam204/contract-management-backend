using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using ContractManagement.Application.AI.Services;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using DomainContract = ContractManagement.Domain.Contract.Entities.Contract;

namespace ContractManagement.UnitTests.AI;

public class AIAnalysisJobServiceTests : IDisposable
{
    private readonly ContractManagementDbContext _context;
    private readonly Mock<IStorageProvider> _storageMock;
    private readonly Mock<IDocumentTextExtractor> _extractorMock;
    private readonly Mock<IAIContractAssistantService> _aiMock;
    private readonly AIAnalysisJobService _sut;

    public AIAnalysisJobServiceTests()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ContractManagementDbContext(options);
        _storageMock = new Mock<IStorageProvider>(MockBehavior.Strict);
        _extractorMock = new Mock<IDocumentTextExtractor>(MockBehavior.Strict);
        _aiMock = new Mock<IAIContractAssistantService>(MockBehavior.Strict);
        _sut = new AIAnalysisJobService(_context, _context, _storageMock.Object, _extractorMock.Object, _aiMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private DomainContract CreateContract(Guid id, string? fileUrl)
    {
        var contract = new DomainContract
        {
            Id = id,
            ContractNumber = "HD-" + id.ToString()[..8],
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Test Contract",
            Value = 1000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = 4,
            FileUrl = fileUrl,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
        return contract;
    }

    private void SetupSuccessMocks(
        string fileUrl,
        byte[] fileBytes,
        string extractedText,
        decimal? value,
        DateTime? expiry,
        string summary)
    {
        _storageMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileBytes);
        _extractorMock.Setup(e => e.ExtractTextAsync(fileUrl, fileBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);
        _aiMock.Setup(a => a.ExtractContractInfoAsync(
                It.Is<ExtractContractRequest>(r => r.ContractContent == extractedText),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedContractInfoDto { Value = value, ExpiryDate = expiry });
        _aiMock.Setup(a => a.SummarizeContractAsync(
                It.Is<SummarizeContractRequest>(r => r.ContractContent == extractedText),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContractSummaryDto { ContractId = Guid.Empty, Summary = summary, KeyPoints = new List<string>() });
    }

    [Fact]
    public async Task AnalyzeContractAsync_WithEmptyContractId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AnalyzeContractAsync(Guid.Empty));
    }

    [Fact]
    public async Task AnalyzeContractAsync_WithNonExistentContract_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AnalyzeContractAsync(id));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeContractAsync_WithMissingFileUrl_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _context.Contracts.Add(CreateContract(id, null));
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AnalyzeContractAsync(id));
        Assert.Contains("file", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeContractAsync_CallsDownloadWithCorrectFileUrl()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "contract.pdf";
        var contract = CreateContract(id, fileUrl);
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1, 2, 3 };
        const string text = "contract text";
        SetupSuccessMocks(fileUrl, fileBytes, text, 5000m, new DateTime(2027, 1, 1), "summary");

        await _sut.AnalyzeContractAsync(id);

        _storageMock.Verify(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeContractAsync_PassesBytesToExtractor()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "contract.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 9, 8, 7 };
        const string text = "extracted text";
        SetupSuccessMocks(fileUrl, fileBytes, text, null, null, "summary");

        await _sut.AnalyzeContractAsync(id);

        _extractorMock.Verify(e => e.ExtractTextAsync(fileUrl, fileBytes, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeContractAsync_CallsExtractAndSummarize()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "contract.docx";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1, 2, 3 };
        const string text = "some contract content";
        SetupSuccessMocks(fileUrl, fileBytes, text, 100m, null, "ai summary");

        await _sut.AnalyzeContractAsync(id);

        _aiMock.Verify(a => a.ExtractContractInfoAsync(
            It.Is<ExtractContractRequest>(r => r.ContractId == id && r.ContractContent == text),
            It.IsAny<CancellationToken>()), Times.Once);
        _aiMock.Verify(a => a.SummarizeContractAsync(
            It.Is<SummarizeContractRequest>(r => r.ContractId == id && r.ContractContent == text),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeContractAsync_CreatesResultWithCorrectContractId()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "file.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        SetupSuccessMocks(fileUrl, new byte[] { 1 }, "text", null, null, "summary");

        await _sut.AnalyzeContractAsync(id);

        var result = await _context.AiAnalysisResults.FirstOrDefaultAsync(r => r.ContractId == id);
        Assert.NotNull(result);
        Assert.Equal(id, result.ContractId);
    }

    [Fact]
    public async Task AnalyzeContractAsync_PersistsValueExpiryAndSummary()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "file.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var expiry = new DateTime(2028, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        SetupSuccessMocks(fileUrl, new byte[] { 1 }, "text", 12345.67m, expiry, "my summary");

        await _sut.AnalyzeContractAsync(id);

        var result = await _context.AiAnalysisResults.FirstAsync(r => r.ContractId == id);
        Assert.Equal(12345.67m, result.ExtractedValue);
        Assert.Equal(expiry, result.ExtractedExpiryDate);
        Assert.Equal("my summary", result.Summary);
    }

    [Fact]
    public async Task AnalyzeContractAsync_CallsSaveChangesAndPersistsOneRow()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "file.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();
        var countBefore = await _context.AiAnalysisResults.CountAsync();

        SetupSuccessMocks(fileUrl, new byte[] { 1 }, "text", null, null, "s");

        await _sut.AnalyzeContractAsync(id);

        var countAfter = await _context.AiAnalysisResults.CountAsync();
        Assert.Equal(countBefore + 1, countAfter);
    }

    [Fact]
    public async Task AnalyzeContractAsync_WithUnsupportedFormat_ThrowsNotSupportedAndDoesNotPersist()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "contract.txt";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1, 2, 3 };
        _storageMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileBytes);
        _extractorMock.Setup(e => e.ExtractTextAsync(fileUrl, fileBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Unsupported document format '.txt'."));

        await Assert.ThrowsAsync<NotSupportedException>(() => _sut.AnalyzeContractAsync(id));

        Assert.Equal(0, await _context.AiAnalysisResults.CountAsync());
        _aiMock.Verify(a => a.ExtractContractInfoAsync(It.IsAny<ExtractContractRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _aiMock.Verify(a => a.SummarizeContractAsync(It.IsAny<SummarizeContractRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeContractAsync_WithEmptyDocument_ThrowsInvalidOperationAndDoesNotPersist()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "empty.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1, 2, 3 };
        _storageMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileBytes);
        _extractorMock.Setup(e => e.ExtractTextAsync(fileUrl, fileBytes, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The document does not contain extractable text."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AnalyzeContractAsync(id));

        Assert.Equal(0, await _context.AiAnalysisResults.CountAsync());
    }

    [Fact]
    public async Task AnalyzeContractAsync_WhenStorageFails_PropagatesAndDoesNotPersist()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "missing.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        _storageMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found: 'missing.pdf'"));

        await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.AnalyzeContractAsync(id));

        Assert.Equal(0, await _context.AiAnalysisResults.CountAsync());
        _extractorMock.Verify(e => e.ExtractTextAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeContractAsync_WhenAiExtractionFails_PropagatesAndDoesNotPersist()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "file.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1 };
        const string text = "text";
        _storageMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(fileBytes);
        _extractorMock.Setup(e => e.ExtractTextAsync(fileUrl, fileBytes, It.IsAny<CancellationToken>())).ReturnsAsync(text);
        _aiMock.Setup(a => a.ExtractContractInfoAsync(It.IsAny<ExtractContractRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI extraction failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AnalyzeContractAsync(id));

        Assert.Equal(0, await _context.AiAnalysisResults.CountAsync());
    }

    [Fact]
    public async Task AnalyzeContractAsync_WhenSummarizeFails_PropagatesAndDoesNotPersist()
    {
        var id = Guid.NewGuid();
        const string fileUrl = "file.pdf";
        _context.Contracts.Add(CreateContract(id, fileUrl));
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1 };
        const string text = "text";
        _storageMock.Setup(s => s.DownloadFileAsync(fileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(fileBytes);
        _extractorMock.Setup(e => e.ExtractTextAsync(fileUrl, fileBytes, It.IsAny<CancellationToken>())).ReturnsAsync(text);
        _aiMock.Setup(a => a.ExtractContractInfoAsync(It.IsAny<ExtractContractRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedContractInfoDto { Value = 1m });
        _aiMock.Setup(a => a.SummarizeContractAsync(It.IsAny<SummarizeContractRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI summarize failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AnalyzeContractAsync(id));

        Assert.Equal(0, await _context.AiAnalysisResults.CountAsync());
    }

    [Fact]
    public async Task AnalyzeContractAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var id = Guid.NewGuid();
        _context.Contracts.Add(CreateContract(id, "file.pdf"));
        await _context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.AnalyzeContractAsync(id, cts.Token));
    }
}
