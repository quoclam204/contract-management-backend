using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Application.Notification.Services;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContractManagement.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void NotificationService_CanBeResolved_WhenDependenciesAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        services.AddScoped(_ => new ContractManagementDbContext(options));
        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<ContractManagementDbContext>());
        services.AddScoped<INotificationService, NotificationService>();

        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetService<INotificationService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<NotificationService>(service);
    }
}
