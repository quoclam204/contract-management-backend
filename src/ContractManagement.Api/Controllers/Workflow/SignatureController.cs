using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Workflow;

/// <summary>
/// Controller quản lý chữ ký điện tử hợp đồng (Signatures - Người 4)
/// Hỗ trợ ký Mock, OTP, và theo dõi tiến độ ký của các bên
/// </summary>
[ApiController]
[Route("api/signatures")]
[Tags("Signatures (Người 4)")]
public class SignatureController : ControllerBase
{
    private readonly ISignatureService _signatureService;

    public SignatureController(ISignatureService signatureService)
    {
        _signatureService = signatureService;
    }

    /// <summary>
    /// Thực hiện ký điện tử cho một bên của hợp đồng (Nội bộ hoặc Đối tác)
    /// Khi tất cả các bên hoàn tất, hệ thống tự động bắn sự kiện ContractSignedEvent
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SignatureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignContract([FromBody] CreateSignatureRequest request)
    {
        try
        {
            var result = await _signatureService.SignContractAsync(request);
            return CreatedAtAction(nameof(GetSignaturesByContractId), new { contractId = result.ContractId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách chữ ký đã ký của một hợp đồng
    /// </summary>
    [HttpGet("contract/{contractId:guid}")]
    [ProducesResponseType(typeof(List<SignatureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignaturesByContractId(Guid contractId)
    {
        var result = await _signatureService.GetSignaturesByContractIdAsync(contractId);
        return Ok(result);
    }

    /// <summary>
    /// Lấy trạng thái tiến độ ký kết của các bên (Nội bộ / Đối tác) cho hợp đồng
    /// </summary>
    [HttpGet("contract/{contractId:guid}/status")]
    [ProducesResponseType(typeof(ContractSignatureStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignatureStatus(Guid contractId)
    {
        var result = await _signatureService.GetSignatureStatusAsync(contractId);
        return Ok(result);
    }
}
