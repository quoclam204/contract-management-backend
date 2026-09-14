using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Identity;

/// <summary>
/// Controller quản lý phòng ban (Department CRUD - Người 1 Lead)
/// </summary>
[ApiController]
[Route("api/departments")]
[Tags("Department Management (Người 1 - Lead)")]
[Authorize(Policy = "RequireManager")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    /// <summary>
    /// Lấy danh sách tất cả phòng ban
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetAllAsync(cancellationToken);
        return Ok(departments);
    }

    /// <summary>
    /// Lấy thông tin chi tiết phòng ban theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdAsync(id, cancellationToken);
        if (department == null)
            return NotFound(new { error = $"Department with ID {id} not found." });

        return Ok(department);
    }

    /// <summary>
    /// Tạo mới một phòng ban
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _departmentService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin phòng ban
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _departmentService.UpdateAsync(id, dto, cancellationToken);
            if (updated == null)
                return NotFound(new { error = $"Department with ID {id} not found." });

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa phòng ban theo Id
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var success = await _departmentService.DeleteAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { error = $"Department with ID {id} not found." });

        return NoContent();
    }
}
