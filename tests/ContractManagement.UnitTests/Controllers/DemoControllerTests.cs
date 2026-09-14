using ContractManagement.Api.Controllers;
using ContractManagement.Application.Common.Messaging;
using ContractManagement.Application.Events.Identity;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ContractManagement.UnitTests.Controllers;

public class DemoControllerTests : IDisposable
{
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly ContractManagementDbContext _dbContext;
    private readonly DemoController _controller;

    public DemoControllerTests()
    {
        _publisherMock = new Mock<IMessagePublisher>();

        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ContractManagementDbContext(options);
        _controller = new DemoController(_publisherMock.Object, _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task PublishUserRegistered_WithNullUserId_PublishesEventAndReturnsOk()
    {
        // Arrange
        UserRegisteredEvent? capturedEvent = null;
        string? capturedExchange = null;
        string? capturedRoutingKey = null;

        _publisherMock
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserRegisteredEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, UserRegisteredEvent, CancellationToken>((ex, rk, evt, ct) =>
            {
                capturedExchange = ex;
                capturedRoutingKey = rk;
                capturedEvent = evt;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PublishUserRegistered(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _publisherMock.Verify(
            p => p.PublishAsync("notification_exchange", "user.registered", It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal("notification_exchange", capturedExchange);
        Assert.Equal("user.registered", capturedRoutingKey);
        Assert.NotNull(capturedEvent);
        Assert.NotEqual(Guid.Empty, capturedEvent!.UserId);
    }

    [Fact]
    public async Task PublishUserRegistered_WithExplicitUserId_PublishesSpecifiedUserId()
    {
        // Arrange
        var explicitUserId = Guid.NewGuid();
        UserRegisteredEvent? capturedEvent = null;

        _publisherMock
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserRegisteredEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, UserRegisteredEvent, CancellationToken>((ex, rk, evt, ct) =>
            {
                capturedEvent = evt;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PublishUserRegistered(explicitUserId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.NotNull(capturedEvent);
        Assert.Equal(explicitUserId, capturedEvent!.UserId);
    }
}
