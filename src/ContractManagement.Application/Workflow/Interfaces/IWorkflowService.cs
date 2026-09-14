using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Workflow.Interfaces;

/// <summary>
/// Interface dịch vụ quản lý cấu hình Workflow và giải quyết luồng duyệt
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Lấy danh sách cấu hình Workflow, có hỗ trợ lọc theo trạng thái hoạt động và tìm kiếm
    /// </summary>
    Task<List<WorkflowDefinitionDto>> GetDefinitionsAsync(bool? isActive = null, string? searchKeyword = null);

    /// <summary>
    /// Lấy chi tiết một cấu hình Workflow theo Id kèm danh sách bước duyệt
    /// </summary>
    Task<WorkflowDefinitionDto?> GetDefinitionByIdAsync(Guid id);

    /// <summary>
    /// Tạo mới một cấu hình Workflow (khởi tạo Version 1)
    /// </summary>
    Task<WorkflowDefinitionDto> CreateDefinitionAsync(CreateWorkflowDefinitionRequest request);

    /// <summary>
    /// Tạo phiên bản mới cho một cấu hình Workflow (Version = MaxVersion + 1)
    /// Bản ghi cũ sẽ được chuyển sang IsActive = false, bản ghi mới có IsActive = true
    /// </summary>
    Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CreateWorkflowVersionRequest request);

    /// <summary>
    /// Cập nhật thông tin cấu hình Workflow
    /// </summary>
    Task<WorkflowDefinitionDto?> UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest request);

    /// <summary>
    /// Bật hoặc tắt trạng thái hoạt động (IsActive) của một phiên bản Workflow
    /// </summary>
    Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive);

    /// <summary>
    /// Xóa cấu hình Workflow (chỉ xóa được nếu chưa có hợp đồng/approval steps tham chiếu)
    /// </summary>
    Task<bool> DeleteDefinitionAsync(Guid id);

    /// <summary>
    /// Tự động tìm kiếm luồng duyệt phù hợp theo giá trị hợp đồng
    /// </summary>
    Task<ResolveWorkflowResponse> ResolveWorkflowForContractAsync(decimal contractValue);

    /// <summary>
    /// Tiện ích kiểm thử tính hợp lệ và thẩm định biểu thức điều kiện với giá trị hợp đồng
    /// </summary>
    EvaluateConditionResponse EvaluateCondition(string expression, decimal contractValue);

    /// <summary>
    /// Thêm một bước vào một cấu hình Workflow đã tồn tại
    /// </summary>
    Task<WorkflowStepDto> AddStepAsync(Guid workflowDefinitionId, CreateWorkflowStepRequest request);
}