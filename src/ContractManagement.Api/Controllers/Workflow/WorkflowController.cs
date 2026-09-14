using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Workflow;

/// <summary>
/// Controller quản lý cấu hình luồng duyệt hợp đồng (Workflow Configuration - Người 4)
/// </summary>
[ApiController]
[Route("api/workflows")]
[Tags("Workflow Configuration (Người 4)")]
[Authorize(Policy = "RequireManager")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    /// <summary>
    /// Lấy danh sách các cấu hình luồng duyệt (hỗ trợ lọc theo trạng thái hoạt động và từ khóa)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkflowDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkflows([FromQuery] bool? isActive, [FromQuery] string? search)
    {
        var result = await _workflowService.GetDefinitionsAsync(isActive, search);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một cấu hình luồng duyệt theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowById(Guid id)
    {
        var result = await _workflowService.GetDefinitionByIdAsync(id);
        if (result == null)
            return NotFound(new { message = $"Không tìm thấy WorkflowDefinition với Id: {id}" });

        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một cấu hình luồng duyệt kèm danh sách các bước duyệt
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowDefinitionRequest request)
    {
        try
        {
            var result = await _workflowService.CreateDefinitionAsync(request);
            return CreatedAtAction(nameof(GetWorkflowById), new { id = result.Id }, result);
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
    /// Tạo phiên bản mới (Versioning) cho luồng duyệt
    /// Phiên bản cũ sẽ tự động tắt (IsActive = false), phiên bản mới được kích hoạt (IsActive = true)
    /// </summary>
    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateNewVersion(Guid id, [FromBody] CreateWorkflowVersionRequest request)
    {
        try
        {
            var result = await _workflowService.CreateNewVersionAsync(id, request);
            return CreatedAtAction(nameof(GetWorkflowById), new { id = result.Id }, result);
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
    /// Cập nhật cấu hình luồng duyệt
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkflow(Guid id, [FromBody] UpdateWorkflowDefinitionRequest request)
    {
        try
        {
            var result = await _workflowService.UpdateDefinitionAsync(id, request);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy WorkflowDefinition với Id: {id}" });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động (IsActive) của một phiên bản luồng duyệt
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromQuery] bool isActive)
    {
        var success = await _workflowService.ToggleActiveStatusAsync(id, isActive);
        if (!success)
            return NotFound(new { message = $"Không tìm thấy WorkflowDefinition với Id: {id}" });

        return Ok(new { message = $"Đã {(isActive ? "bật" : "tắt")} trạng thái hoạt động của Workflow Id: {id}" });
    }

    /// <summary>
    /// Xóa cấu hình luồng duyệt (chỉ xóa được khi chưa có hợp đồng nào sử dụng)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkflow(Guid id)
    {
        try
        {
            var success = await _workflowService.DeleteDefinitionAsync(id);
            if (!success)
                return NotFound(new { message = $"Không tìm thấy WorkflowDefinition với Id: {id}" });

            return Ok(new { message = $"Đã xóa thành công WorkflowDefinition Id: {id}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Tự động xác định luồng duyệt phù hợp cho một giá trị hợp đồng
    /// </summary>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(ResolveWorkflowResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveWorkflow([FromBody] ResolveWorkflowRequest request)
    {
        var result = await _workflowService.ResolveWorkflowForContractAsync(request.ContractValue);
        return Ok(result);
    }

    /// <summary>
    /// Kiểm tra và thẩm định biểu thức điều kiện giá trị hợp đồng
    /// </summary>
    [HttpPost("evaluate-condition")]
    [ProducesResponseType(typeof(EvaluateConditionResponse), StatusCodes.Status200OK)]
    public IActionResult EvaluateCondition([FromBody] EvaluateConditionRequest request)
    {
        var result = _workflowService.EvaluateCondition(request.Expression, request.ContractValue);
        return Ok(result);
    }

    /// <summary>
    /// Thêm một bước vào một luồng duyệt đã tồn tại
    /// </summary>
    [HttpPost("{workflowId:guid}/steps")]
    [ProducesResponseType(typeof(WorkflowStepDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStep(Guid workflowId, [FromBody] CreateWorkflowStepRequest request)
    {
        try
        {
            var result = await _workflowService.AddStepAsync(workflowId, request);
            return CreatedAtAction(nameof(GetWorkflowById), new { id = result.Id }, result);
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
}