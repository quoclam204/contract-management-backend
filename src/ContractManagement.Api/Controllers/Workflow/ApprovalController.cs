using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Workflow;

/// <summary>
/// Controller xử lý tiến trình phê duyệt hợp đồng (Submit, Approve, Reject - Người 4)
/// </summary>
[ApiController]
[Route("api/approvals")]
[Tags("Approval Process")]
[Authorize(Policy = "RequireApprover")]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    /// <summary>
    /// Đệ trình hợp đồng vào tiến trình phê duyệt (Submit)
    /// Hệ thống sẽ tự động xác định luồng duyệt theo giá trị nếu không chỉ định và tạo snapshot các bước duyệt
    /// </summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ContractApprovalProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForApproval([FromBody] SubmitContractApprovalRequest request)
    {
        try
        {
            var result = await _approvalService.SubmitForApprovalAsync(request);
            return Ok(result);
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
    /// Ra quyết định phê duyệt (Approve = 1) hoặc từ chối (Reject = 2) cho một bước duyệt
    /// Khi toàn bộ bước Pass -> Bắn sự kiện WorkflowApprovedEvent
    /// Khi một bước Reject -> Bắn sự kiện WorkflowRejectedEvent
    /// </summary>
    [HttpPost("decision")]
    [ProducesResponseType(typeof(ContractApprovalProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessDecision([FromBody] ProcessApprovalDecisionRequest request)
    {
        try
        {
            var result = await _approvalService.ProcessDecisionAsync(request);
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết tiến trình và lịch sử phê duyệt của một hợp đồng
    /// </summary>
    [HttpGet("contract/{contractId:guid}")]
    [ProducesResponseType(typeof(ContractApprovalProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgressByContractId(Guid contractId)
    {
        var result = await _approvalService.GetProgressByContractIdAsync(contractId);
        if (result == null)
            return NotFound(new { message = $"Chưa có tiến trình phê duyệt nào cho hợp đồng Id: {contractId}" });

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các bước đang chờ duyệt (phục vụ màn hình danh sách chờ duyệt)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PendingApprovalItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] Guid? approverId)
    {
        var result = await _approvalService.GetPendingApprovalsAsync(approverId);
        return Ok(result);
    }
}
