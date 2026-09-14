using ContractManagement.Application.Contracts.DTOs;
using ContractManagement.Application.Contracts.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Contracts;

/// <summary>
/// Controller quản lý phiên bản mẫu hợp đồng
/// </summary>
[ApiController]
[Route("api/contract-template-versions")]
[Tags("Contract Template Versions")]
public class ContractTemplateVersionController : ControllerBase
{
    private readonly IContractTemplateVersionService _contractTemplateVersionService;

    public ContractTemplateVersionController(IContractTemplateVersionService contractTemplateVersionService)
    {
        _contractTemplateVersionService = contractTemplateVersionService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các phiên bản mẫu hợp đồng
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ContractTemplateVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractTemplateVersions()
    {
        var result = await _contractTemplateVersionService.GetContractTemplateVersionsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một phiên bản mẫu hợp đồng theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContractTemplateVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractTemplateVersionById(Guid id)
    {
        var result = await _contractTemplateVersionService.GetContractTemplateVersionByIdAsync(id);
        if (result == null)
            return NotFound(new { message = $"Không tìm thấy phiên bản mẫu hợp đồng với Id: {id}" });

        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một phiên bản mẫu hợp đồng
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContractTemplateVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContractTemplateVersion([FromBody] CreateContractTemplateVersionRequest request)
    {
        try
        {
            var result = await _contractTemplateVersionService.CreateContractTemplateVersionAsync(request);
            return CreatedAtAction(nameof(GetContractTemplateVersionById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật một phiên bản mẫu hợp đồng
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContractTemplateVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContractTemplateVersion(Guid id, [FromBody] UpdateContractTemplateVersionRequest request)
    {
        try
        {
            var result = await _contractTemplateVersionService.UpdateContractTemplateVersionAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa một phiên bản mẫu hợp đồng
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContractTemplateVersion(Guid id)
    {
        var result = await _contractTemplateVersionService.DeleteContractTemplateVersionAsync(id);
        if (!result)
            return NotFound(new { message = $"Không tìm thấy phiên bản mẫu hợp đồng với Id: {id}" });

        return Ok(new { message = $"Đã xóa thành công phiên bản mẫu hợp đồng Id: {id}" });
    }
}