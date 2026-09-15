using ContractManagement.Api.Controllers.AI;
using ContractManagement.Application.AI.DTOs;
using ContractManagement.Application.AI.Interfaces;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Domain.AI.Entities;
using ContractManagement.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DomainContract = ContractManagement.Domain.Contract.Entities.Contract;

namespace ContractManagement.UnitTests.Controllers.AI;

public class AIContractAssistantAnalysisTests : IDisposable
{
    private readonly ContractManagementDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IBackgroundJobClient> _bgMock;
    private readonly Mock<IAIContractAssistantService> _aiServiceMock;
    private readonly AIContractAssistantController _sut;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _contractId = Guid.NewGuid();

    public AIContractAssistantAnalysisTests()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ContractManagementDbContext(options);
        _currentUserMock = new Mock<ICurrentUserService>();
        _bgMock = new Mock<IBackgroundJobClient>();
        _aiServiceMock = new Mock<IAIContractAssistantService>();

        // Default: authenticated as owner
        _currentUserMock.SetupGet(x => x.UserId).Returns(_ownerId);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(true);

        _sut = new AIContractAssistantController(
            _aiServiceMock.Object,
            _context,
            _context,
            _currentUserMock.Object,
            _bgMock.Object,
            NullLogger<AIContractAssistantController>.Instance);

        // Need HttpContext for controller; not strictly required but set for completeness
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        SeedContract(_contractId, _ownerId);
    }

    public void Dispose() => _context.Dispose();

    private void SeedContract(Guid id, Guid ownerId)
    {
        ((IContractManagementDbContext)_context).Contracts.Add(new DomainContract
        {
            Id = id,
            ContractNumber = "HD-" + id.ToString()[..8],
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = "Test Contract",
            Value = 1000m,
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = 4,
            FileUrl = "contract.pdf",
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        });
        _context.SaveChanges();
    }

    // ------------------------------------------------------------------
    // Enqueue tests
    // ------------------------------------------------------------------

    [Fact]
    public async Task Enqueue_AuthenticatedOwner_Returns202AndEnqueues()
    {
        var result = await _sut.AnalyzeContractById(_contractId);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);

        _bgMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Type == typeof(IAIAnalysisJobService)
                && job.Method.Name == nameof(IAIAnalysisJobService.AnalyzeContractAsync)
                && (Guid)job.Args[0]! == _contractId),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task Enqueue_CorrectContractId_PassedToJob()
    {
        var specificId = _contractId;
        await _sut.AnalyzeContractById(specificId);

        _bgMock.Verify(x => x.Create(
            It.Is<Job>(job => (Guid)job.Args[0]! == specificId),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task Enqueue_UnauthorizedUser_ForOtherOwnersContract_Returns403()
    {
        _currentUserMock.SetupGet(x => x.UserId).Returns(_otherUserId);

        var result = await _sut.AnalyzeContractById(_contractId);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        _bgMock.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task Enqueue_Unauthenticated_Returns401()
    {
        _currentUserMock.SetupGet(x => x.UserId).Returns((Guid?)null);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(false);

        var result = await _sut.AnalyzeContractById(_contractId);

        var obj = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, obj.StatusCode);
    }

    [Fact]
    public async Task Enqueue_NonExistentContract_Returns404()
    {
        var missing = Guid.NewGuid();
        var result = await _sut.AnalyzeContractById(missing);

        var obj = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }

    [Fact]
    public async Task Enqueue_EmptyGuid_Returns400()
    {
        var result = await _sut.AnalyzeContractById(Guid.Empty);
        var obj = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
    }

    // ------------------------------------------------------------------
    // Retrieval tests
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetAnalysis_AuthenticatedOwner_ReturnsLatestByAnalyzedAt()
    {
        var older = new AiAnalysisResult(_contractId, "older", 100m, null, null) { AnalyzedAt = DateTime.UtcNow.AddHours(-2) };
        var newer = new AiAnalysisResult(_contractId, "newer", 200m, null, null) { AnalyzedAt = DateTime.UtcNow };
        _context.AiAnalysisResults.AddRange(older, newer);
        await _context.SaveChangesAsync();

        var result = await _sut.GetAnalysis(_contractId);

        var ok = Assert.IsType<OkObjectResult>(result.Result!);
        var dto = Assert.IsType<AiAnalysisResultDto>(ok.Value);
        Assert.Equal("newer", dto.Summary);
        Assert.Equal(200m, dto.ExtractedValue);
    }

    [Fact]
    public async Task GetAnalysis_NoAnalysis_Returns404()
    {
        var result = await _sut.GetAnalysis(_contractId);
        var obj = Assert.IsType<NotFoundObjectResult>(result.Result!);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_UnauthorizedUser_ForOtherOwnersContract_Returns403()
    {
        _context.AiAnalysisResults.Add(new AiAnalysisResult(_contractId, "s", 1m, null, null));
        await _context.SaveChangesAsync();

        _currentUserMock.SetupGet(x => x.UserId).Returns(_otherUserId);

        var result = await _sut.GetAnalysis(_contractId);
        var obj = Assert.IsType<ObjectResult>(result.Result!);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_NonExistentContract_Returns404()
    {
        var result = await _sut.GetAnalysis(Guid.NewGuid());
        var obj = Assert.IsType<NotFoundObjectResult>(result.Result!);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_EmptyGuid_Returns400()
    {
        var result = await _sut.GetAnalysis(Guid.Empty);
        var obj = Assert.IsType<BadRequestObjectResult>(result.Result!);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_Unauthenticated_Returns401()
    {
        _currentUserMock.SetupGet(x => x.UserId).Returns((Guid?)null);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(false);

        var result = await _sut.GetAnalysis(_contractId);
        var obj = Assert.IsType<UnauthorizedObjectResult>(result.Result!);
        Assert.Equal(StatusCodes.Status401Unauthorized, obj.StatusCode);
    }
}
