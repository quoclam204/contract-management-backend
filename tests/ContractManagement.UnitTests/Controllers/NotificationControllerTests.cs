using ContractManagement.Api.Controllers;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Notification.DTOs;
using ContractManagement.Application.Notification.Interfaces;
using ContractManagement.Domain.Notification.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ContractManagement.UnitTests.Controllers;

public class NotificationControllerTests
{
    private static (NotificationController controller, Mock<INotificationService> svcMock, Mock<ICurrentUserService> userMock) CreateSut(Guid? userId)
    {
        var svcMock = new Mock<INotificationService>();
        var userMock = new Mock<ICurrentUserService>();
        userMock.SetupGet(x => x.UserId).Returns(userId);
        var controller = new NotificationController(svcMock.Object, userMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return (controller, svcMock, userMock);
    }

    [Fact]
    public void NotificationController_HasAuthorizeAttribute()
    {
        var attrs = typeof(NotificationController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);
        Assert.NotEmpty(attrs);
    }

    [Fact]
    public async Task GetMyNotifications_Authenticated_Returns200()
    {
        var userId = Guid.NewGuid();
        var (controller, svcMock, _) = CreateSut(userId);
        svcMock.Setup(s => s.GetByUserIdAsync(userId, null)).ReturnsAsync(new List<NotificationDto>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.ApprovalRequest }
        });

        var result = await controller.GetMyNotifications(null);

        var ok = Assert.IsType<OkObjectResult>(result);
        svcMock.Verify(s => s.GetByUserIdAsync(userId, null), Times.Once);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetMyNotifications_Unauthenticated_Returns401()
    {
        var (controller, svcMock, _) = CreateSut(null);

        var result = await controller.GetMyNotifications(null);

        Assert.IsType<UnauthorizedObjectResult>(result);
        svcMock.Verify(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>()), Times.Never);
    }

    [Fact]
    public async Task GetMyNotifications_XUserIdHeader_MustNotBeUsed_Returns401EvenWhenHeaderPresent()
    {
        var (controller, svcMock, _) = CreateSut(null);
        controller.HttpContext.Request.Headers["X-User-Id"] = Guid.NewGuid().ToString();
        controller.HttpContext.Request.Headers["X-User-ID"] = Guid.NewGuid().ToString();
        // also lowercase variant
        controller.HttpContext.Request.Headers["x-user-id"] = Guid.NewGuid().ToString();

        var result = await controller.GetMyNotifications(null);

        Assert.IsType<UnauthorizedObjectResult>(result);
        svcMock.Verify(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<bool?>()), Times.Never);
        // proves header is ignored — authenticated identity comes only from ICurrentUserService
    }

    [Fact]
    public async Task GetUnreadCount_Authenticated_Returns200()
    {
        var userId = Guid.NewGuid();
        var (controller, svcMock, _) = CreateSut(userId);
        svcMock.Setup(s => s.GetUnreadCountAsync(userId)).ReturnsAsync(3);

        var result = await controller.GetUnreadCount();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(3, ok.Value);
    }

    [Fact]
    public async Task GetUnreadCount_Unauthenticated_Returns401()
    {
        var (controller, svcMock, _) = CreateSut(Guid.Empty); // Guid.Empty also maps to unauthenticated via controller check

        // Use null UserId path as well — both should be 401
        var (controller2, svcMock2, _) = CreateSut(null);
        Assert.IsType<UnauthorizedObjectResult>(await controller2.GetUnreadCount());
        Assert.IsType<UnauthorizedObjectResult>(await controller.GetUnreadCount());
        svcMock.Verify(s => s.GetUnreadCountAsync(It.IsAny<Guid>()), Times.Never);
        svcMock2.Verify(s => s.GetUnreadCountAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsRead_DelegatesWithOwnership_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var (controller, svcMock, _) = CreateSut(userId);
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        svcMock.Setup(s => s.MarkAsReadAsync(userId, ids)).ReturnsAsync(2);

        var result = await controller.MarkAsRead(new MarkNotificationsReadRequest { NotificationIds = ids });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ok.Value);
        svcMock.Verify(s => s.MarkAsReadAsync(userId, It.Is<List<Guid>>(l => l.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_XUserIdHeader_Ignored_UsesOnlyCurrentUserService()
    {
        var realUserId = Guid.NewGuid();
        var fakeHeaderUserId = Guid.NewGuid();
        var (controller, svcMock, _) = CreateSut(realUserId);
        controller.HttpContext.Request.Headers["X-User-Id"] = fakeHeaderUserId.ToString();
        var ids = new List<Guid> { Guid.NewGuid() };
        svcMock.Setup(s => s.MarkAsReadAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>())).ReturnsAsync(1);

        await controller.MarkAsRead(new MarkNotificationsReadRequest { NotificationIds = ids });

        svcMock.Verify(s => s.MarkAsReadAsync(realUserId, It.IsAny<List<Guid>>()), Times.Once);
        svcMock.Verify(s => s.MarkAsReadAsync(fakeHeaderUserId, It.IsAny<List<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsRead_Unauthenticated_Returns401()
    {
        var (controller, svcMock, _) = CreateSut(null);
        var result = await controller.MarkAsRead(new MarkNotificationsReadRequest { NotificationIds = new List<Guid> { Guid.NewGuid() } });
        Assert.IsType<UnauthorizedObjectResult>(result);
        svcMock.Verify(s => s.MarkAsReadAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task MarkAllAsRead_Authenticated_DelegatesOwnership()
    {
        var userId = Guid.NewGuid();
        var (controller, svcMock, _) = CreateSut(userId);
        svcMock.Setup(s => s.MarkAllAsReadAsync(userId)).ReturnsAsync(5);

        var result = await controller.MarkAllAsRead();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(5, ok.Value);
        svcMock.Verify(s => s.MarkAllAsReadAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUnreadNotifications_Authenticated_Returns200()
    {
        var userId = Guid.NewGuid();
        var (controller, svcMock, _) = CreateSut(userId);
        svcMock.Setup(s => s.GetUnreadByUserIdAsync(userId)).ReturnsAsync(new List<NotificationDto>());

        var result = await controller.GetUnreadNotifications();

        Assert.IsType<OkObjectResult>(result);
        svcMock.Verify(s => s.GetUnreadByUserIdAsync(userId), Times.Once);
    }
}
