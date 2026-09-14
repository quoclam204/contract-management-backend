using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Identity;

/// <summary>
/// Controller quản lý danh sách người dùng
/// </summary>
[ApiController]
[Route("api/users")]
[Tags("User Management")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ người dùng trong hệ thống (Yêu cầu quyền Manager trở lên)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _authService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Lấy chi tiết thông tin người dùng theo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _authService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new { error = $"User with ID {id} not found." });

        return Ok(user);
    }

    /// <summary>
    /// Tạo người dùng mới với vai trò chỉ định (Chỉ Admin mới có quyền)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
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
}
