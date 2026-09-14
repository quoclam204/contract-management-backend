using ContractManagement.Application.Common.Models;
using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Identity;

/// <summary>
/// Controller quản trị người dùng (User Management)
/// </summary>
[ApiController]
[Route("api/users")]
[Tags("User Management")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lấy danh sách người dùng có phân trang và bộ lọc (Yêu cầu quyền Manager trở lên)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(PagedResult<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] UserFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết thông tin người dùng theo ID (Yêu cầu quyền Manager trở lên)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new { error = $"User with ID '{id}' not found." });

        return Ok(user);
    }

    /// <summary>
    /// Cập nhật thông tin người dùng (Yêu cầu quyền Admin)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updatedUser = await _userService.UpdateUserAsync(id, dto, cancellationToken);
            if (updatedUser == null)
                return NotFound(new { error = $"User with ID '{id}' not found." });

            return Ok(updatedUser);
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
    /// Vô hiệu hóa (khóa) tài khoản người dùng (Yêu cầu quyền Admin)
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _userService.DeactivateUserAsync(id, cancellationToken);
            if (!success)
                return NotFound(new { error = $"User with ID '{id}' not found." });

            return Ok(new { message = "User deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của tài khoản người dùng (Yêu cầu quyền Admin)
    /// </summary>
    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _userService.ToggleUserActiveAsync(id, cancellationToken);
            if (!success)
                return NotFound(new { error = $"User with ID '{id}' not found." });

            return Ok(new { message = "User status updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách người dùng theo phòng ban (Yêu cầu quyền Manager trở lên)
    /// </summary>
    [HttpGet("by-department/{departmentId:guid}")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(IEnumerable<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDepartment(Guid departmentId, CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersByDepartmentAsync(departmentId, cancellationToken);
        return Ok(users);
    }
}
