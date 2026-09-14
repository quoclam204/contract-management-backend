using ContractManagement.Application.Notification.DTOs;
using ContractManagement.Application.Notification.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers;

/// <summary>
/// Controller xử lý thông báo người dùng (Notification)
/// </summary>
[ApiController]
[Route("api/notifications")]
[Tags("Notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại
    /// </summary>
    /// <param name="isRead">Lọc theo trạng thái đọc (true = đã đọc, false = chưa đọc). Không truyền để lấy tất cả.</param>
    /// <returns>Danh sách thông báo</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool? isRead = null)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User identity not found. Provide X-User-Id header." });

        var notifications = await _notificationService.GetByUserIdAsync(userId, isRead);
        return Ok(notifications);
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc của người dùng hiện tại
    /// </summary>
    /// <returns>Số lượng thông báo chưa đọc</returns>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User identity not found. Provide X-User-Id header." });

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    /// <summary>
    /// Lấy danh sách thông báo chưa đọc của người dùng hiện tại
    /// </summary>
    /// <returns>Danh sách thông báo chưa đọc</returns>
    [HttpGet("unread")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User identity not found. Provide X-User-Id header." });

        var notifications = await _notificationService.GetUnreadByUserIdAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// Đánh dấu các thông báo đã chọn là đã đọc
    /// </summary>
    /// <param name="request">Danh sách ID thông báo cần đánh dấu đã đọc</param>
    /// <returns>Số lượng thông báo đã được cập nhật</returns>
    [HttpPatch("mark-read")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkNotificationsReadRequest request)
    {
        if (request?.NotificationIds == null || request.NotificationIds.Count == 0)
            return BadRequest(new { error = "NotificationIds không được để trống." });

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User identity not found. Provide X-User-Id header." });

        var affected = await _notificationService.MarkAsReadAsync(userId, request.NotificationIds);
        return Ok(affected);
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc
    /// </summary>
    /// <returns>Số lượng thông báo đã được cập nhật</returns>
    [HttpPatch("mark-all-read")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "User identity not found. Provide X-User-Id header." });

        var affected = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(affected);
    }

    /// <summary>
    /// Lấy UserId từ header X-User-Id (tạm thời cho đến khi có authentication đầy đủ)
    /// Trong môi trường production, sẽ lấy từ ClaimsPrincipal sau khi có JWT authentication
    /// </summary>
    private Guid GetCurrentUserId()
    {
        // Ưu tiên lấy từ ClaimsPrincipal (khi đã có authentication)
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("userId");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userIdFromClaim))
            return userIdFromClaim;

        // Fallback: Lấy từ header X-User-Id (cho development/testing)
        if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader)
            && Guid.TryParse(userIdHeader.FirstOrDefault(), out var userIdFromHeader))
            return userIdFromHeader;

        return Guid.Empty;
    }
}