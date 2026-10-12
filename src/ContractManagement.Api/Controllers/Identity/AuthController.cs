using System.IO;
using System.Linq;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Identity;

/// <summary>
/// Controller xử lý xác thực và phân quyền người dùng (Auth và Identity - Người 1 Lead)
/// </summary>
[ApiController]
[Route("api/auth")]
[Tags("Authentication & Identity")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Đăng nhập hệ thống và nhận JWT Access Token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cấp mới JWT Token từ Refresh Token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Đăng ký tài khoản người dùng mới
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.RegisterAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin tài khoản hiện tại từ JWT Token
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new { error = "Not authenticated." });

        var user = await _authService.GetUserByIdAsync(_currentUserService.UserId.Value, cancellationToken);
        if (user == null)
            return NotFound(new { error = "User not found." });

        return Ok(user);
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân (Đổi họ tên)
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new { error = "Not authenticated." });

        try
        {
            var updated = await _authService.UpdateProfileAsync(_currentUserService.UserId.Value, request.FullName, request.AvatarUrl, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản cá nhân
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new { error = "Not authenticated." });

        try
        {
            await _authService.ChangePasswordAsync(_currentUserService.UserId.Value, request.CurrentPassword, request.NewPassword, cancellationToken);
            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Tải lên ảnh đại diện cá nhân (avatar)
    /// </summary>
    [HttpPost("avatar")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new { error = "Not authenticated." });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Tệp ảnh không được để trống." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { error = "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp, .gif" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Kích thước ảnh tối đa là 5MB." });

        var userId = _currentUserService.UserId.Value;
        var avatarsDir = Path.Combine(Directory.GetCurrentDirectory(), "storage", "avatars");
        if (!Directory.Exists(avatarsDir))
            Directory.CreateDirectory(avatarsDir);

        var safeFileName = $"{userId}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(avatarsDir, safeFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var avatarUrl = $"/api/auth/avatar/{safeFileName}";
        return Ok(new { avatarUrl });
    }

    /// <summary>
    /// Lấy ảnh đại diện
    /// </summary>
    [HttpGet("avatar/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetAvatar(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var avatarsDir = Path.Combine(Directory.GetCurrentDirectory(), "storage", "avatars");
        var filePath = Path.Combine(avatarsDir, safeFileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType);
    }
}
