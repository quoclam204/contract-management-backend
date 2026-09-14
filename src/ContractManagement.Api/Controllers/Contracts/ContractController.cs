using ContractManagement.Application.Contracts.DTOs;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Contracts.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Contracts;

/// <summary>
/// Controller quản lý hợp đồng
/// </summary>
[ApiController]
[Route("api/contracts")]
[Tags("Contracts")]
public class ContractController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractController(IContractService contractService)
    {
        _contractService = contractService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các hợp đồng
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts()
    {
        var result = await _contractService.GetContractsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một hợp đồng theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractById(Guid id)
    {
        var result = await _contractService.GetContractByIdAsync(id);
        if (result == null)
            return NotFound(new { message = $"Không tìm thấy hợp đồng với Id: {id}" });

        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một hợp đồng
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        try
        {
            var result = await _contractService.CreateContractAsync(request);
            return CreatedAtAction(nameof(GetContractById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật một hợp đồng
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractRequest request)
    {
        try
        {
            var result = await _contractService.UpdateContractAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa một hợp đồng
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContract(Guid id)
    {
        var result = await _contractService.DeleteContractAsync(id);
        if (!result)
            return NotFound(new { message = $"Không tìm thấy hợp đồng với Id: {id}" });

        return Ok(new { message = $"Đã xóa thành công hợp đồng Id: {id}" });
    }

    /// <summary>
    /// Đệ trình hợp đồng vào tiến trình phê duyệt (Submit)
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitContract(Guid id)
    {
        try
        {
            await _contractService.SubmitContractAsync(id);
            return Ok(new { message = $"Đã đợt trình hợp đồng Id: {id} để phê duyệt" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}