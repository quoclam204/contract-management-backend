using ContractManagement.Application.Common.Messaging;
using ContractManagement.Application.Events.Identity;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Api.Controllers;

/// <summary>
/// Demo controller for testing/demonstrating RabbitMQ message publishing.
/// </summary>
[ApiController]
[Route("api/demo")]
[Tags("Demo")]
public class DemoController : ControllerBase
{
    private const string ExchangeName = "notification_exchange";
    private const string RoutingKey = "user.registered";

    private readonly IMessagePublisher _messagePublisher;
    private readonly ContractManagementDbContext _dbContext;

    public DemoController(
        IMessagePublisher messagePublisher,
        ContractManagementDbContext dbContext)
    {
        _messagePublisher = messagePublisher;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Publishes a UserRegisteredEvent to RabbitMQ to demonstrate end-to-end notification delivery.
    /// Guarantees that the target UserId exists in dbo.USERS to satisfy the FK_NOTIFICATIONS_USERS constraint.
    /// </summary>
    /// <param name="userId">Optional explicit UserId. If provided, ensures that user exists in dbo.USERS. If omitted, uses an existing user or creates a demo user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated/selected UserId and published status.</returns>
    [HttpPost("publish-user-registered")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishUserRegistered([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var targetUserId = await EnsureUserExistsAsync(userId, cancellationToken);

        var @event = new UserRegisteredEvent(targetUserId);

        await _messagePublisher.PublishAsync(ExchangeName, RoutingKey, @event, cancellationToken);

        return Ok(new
        {
            message = "UserRegisteredEvent published successfully",
            userId = targetUserId,
            exchange = ExchangeName,
            routingKey = RoutingKey
        });
    }

    private async Task<Guid> EnsureUserExistsAsync(Guid? requestedUserId, CancellationToken cancellationToken)
    {
        // For non-relational database providers (e.g. Unit tests with EF Core InMemory), bypass raw SQL execution
        if (!_dbContext.Database.IsRelational())
        {
            return requestedUserId.HasValue && requestedUserId.Value != Guid.Empty
                ? requestedUserId.Value
                : Guid.NewGuid();
        }

        // 1. If explicit requestedUserId provided, check if it exists in dbo.USERS or create it with that exact Id
        if (requestedUserId.HasValue && requestedUserId.Value != Guid.Empty)
        {
            var targetId = requestedUserId.Value;
            var exists = await CheckUserExistsAsync(targetId, cancellationToken);
            if (exists)
            {
                return targetId;
            }

            await InsertDemoUserAsync(targetId, cancellationToken);
            return targetId;
        }

        // 2. If no requestedUserId provided (or empty Guid), pick an existing user in dbo.USERS or create one
        var existingUserId = await GetAnyExistingUserIdAsync(cancellationToken);
        if (existingUserId != Guid.Empty)
        {
            return existingUserId;
        }

        var newUserId = Guid.NewGuid();
        await InsertDemoUserAsync(newUserId, cancellationToken);
        return newUserId;
    }

    private async Task<bool> CheckUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var foundId = await _dbContext.Database
            .SqlQueryRaw<Guid>("SELECT Id AS [Value] FROM dbo.USERS WHERE Id = {0}", userId)
            .FirstOrDefaultAsync(cancellationToken);

        return foundId == userId;
    }

    private async Task<Guid> GetAnyExistingUserIdAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Database
            .SqlQueryRaw<Guid>("SELECT TOP 1 Id AS [Value] FROM dbo.USERS")
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task InsertDemoUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var email = $"demo_{userId:N}@example.com";
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO dbo.USERS (Id, FullName, Email, PasswordHash, Role) VALUES ({0}, {1}, {2}, {3}, {4})",
            userId, "Demo User", email, "demo_hash", (byte)2, cancellationToken);
    }
}
