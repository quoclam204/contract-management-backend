using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Workflow.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowDbContext _context;
    private readonly IWorkflowConditionEvaluator _evaluator;

    public WorkflowService(IWorkflowDbContext context, IWorkflowConditionEvaluator evaluator)
    {
        _context = context;
        _evaluator = evaluator;
    }

    public async Task<List<WorkflowDefinitionDto>> GetDefinitionsAsync(bool? isActive = null, string? searchKeyword = null)
    {
        var query = _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
            .AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var keyword = searchKeyword.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(keyword) ||
                                     (w.ConditionExpression != null && w.ConditionExpression.ToLower().Contains(keyword)));
        }

        var list = await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<WorkflowDefinitionDto?> GetDefinitionByIdAsync(Guid id)
    {
        var definition = await _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id);

        return definition == null ? null : MapToDto(definition);
    }

    public async Task<WorkflowDefinitionDto> CreateDefinitionAsync(CreateWorkflowDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên luồng duyệt không được để trống.", nameof(request.Name));

        var name = request.Name.Trim();

        // Kiểm tra tính hợp lệ của biểu thức điều kiện
        if (!_evaluator.TryValidate(request.ConditionExpression, out var syntaxError))
            throw new ArgumentException(syntaxError, nameof(request.ConditionExpression));

        // Kiểm tra trùng tên đối với bản ghi đang Active
        if (request.IsActive && await _context.WorkflowDefinitions.AnyAsync(w => w.Name == name && w.IsActive))
        {
            throw new InvalidOperationException($"Đã tồn tại luồng duyệt có tên '{name}' đang hoạt động (Active). Vui lòng tạo phiên bản mới (New Version) hoặc tắt luồng duyệt cũ.");
        }

        // Kiểm tra danh sách các bước duyệt
        ValidateSteps(request.Steps);

        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            ConditionExpression = string.IsNullOrWhiteSpace(request.ConditionExpression) ? null : request.ConditionExpression.Trim(),
            Version = 1,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var s in request.Steps.OrderBy(s => s.StepOrder))
        {
            definition.WorkflowSteps.Add(new WorkflowStep
            {
                Id = Guid.NewGuid(),
                WorkflowDefinitionId = definition.Id,
                StepOrder = s.StepOrder,
                ApproverRole = s.ApproverRole,
                IsRequired = s.IsRequired,
                MinimumAmount = s.MinimumAmount
            });
        }

        _context.WorkflowDefinitions.Add(definition);
        await _context.SaveChangesAsync();

        return MapToDto(definition);
    }

    public async Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CreateWorkflowVersionRequest request)
    {
        var original = await _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (original == null)
            throw new KeyNotFoundException($"Không tìm thấy WorkflowDefinition có Id: {id}");

        var conditionExpression = request.ConditionExpression ?? original.ConditionExpression;
        if (!_evaluator.TryValidate(conditionExpression, out var syntaxError))
            throw new ArgumentException(syntaxError, nameof(request.ConditionExpression));

        // Nếu không truyền steps mới thì kế thừa từ version cũ
        var stepsToApply = request.Steps != null && request.Steps.Any()
            ? request.Steps
            : original.WorkflowSteps.Select(s => new CreateWorkflowStepRequest
            {
                StepOrder = s.StepOrder,
                ApproverRole = s.ApproverRole,
                IsRequired = s.IsRequired,
                MinimumAmount = s.MinimumAmount
            }).ToList();

        ValidateSteps(stepsToApply);

        // Tìm phiên bản cao nhất của luồng duyệt này
        var maxVersion = await _context.WorkflowDefinitions
            .Where(w => w.Name == original.Name)
            .MaxAsync(w => w.Version);

        // Tắt toàn bộ phiên bản cũ đang Active của luồng duyệt có cùng Name
        var activeExisting = await _context.WorkflowDefinitions
            .Where(w => w.Name == original.Name && w.IsActive)
            .ToListAsync();

        foreach (var item in activeExisting)
        {
            item.IsActive = false;
        }

        // Tạo bản ghi version mới
        var newVersion = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = original.Name,
            ConditionExpression = string.IsNullOrWhiteSpace(conditionExpression) ? null : conditionExpression.Trim(),
            Version = maxVersion + 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var s in stepsToApply.OrderBy(s => s.StepOrder))
        {
            newVersion.WorkflowSteps.Add(new WorkflowStep
            {
                Id = Guid.NewGuid(),
                WorkflowDefinitionId = newVersion.Id,
                StepOrder = s.StepOrder,
                ApproverRole = s.ApproverRole,
                IsRequired = s.IsRequired,
                MinimumAmount = s.MinimumAmount
            });
        }

        _context.WorkflowDefinitions.Add(newVersion);
        await _context.SaveChangesAsync();

        return MapToDto(newVersion);
    }

    public async Task<WorkflowDefinitionDto?> UpdateDefinitionAsync(Guid id, UpdateWorkflowDefinitionRequest request)
    {
        var definition = await _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(w => w.Id == id);

        if (definition == null)
            return null;

        if (request.ConditionExpression != null)
        {
            if (!_evaluator.TryValidate(request.ConditionExpression, out var syntaxError))
                throw new ArgumentException(syntaxError, nameof(request.ConditionExpression));

            definition.ConditionExpression = string.IsNullOrWhiteSpace(request.ConditionExpression)
                ? null
                : request.ConditionExpression.Trim();
        }

        if (request.IsActive.HasValue && request.IsActive.Value != definition.IsActive)
        {
            if (request.IsActive.Value)
            {
                // Tắt các bản ghi khác có cùng tên để bảo đảm unique index UX_WFDEF_Name_Active
                var activeOthers = await _context.WorkflowDefinitions
                    .Where(w => w.Name == definition.Name && w.Id != definition.Id && w.IsActive)
                    .ToListAsync();

                foreach (var other in activeOthers)
                    other.IsActive = false;
            }

            definition.IsActive = request.IsActive.Value;
        }

        await _context.SaveChangesAsync();
        return MapToDto(definition);
    }

    public async Task<bool> ToggleActiveStatusAsync(Guid id, bool isActive)
    {
        var definition = await _context.WorkflowDefinitions.FindAsync(id);
        if (definition == null)
            return false;

        if (isActive)
        {
            // Tắt các bản ghi khác cùng Name
            var activeOthers = await _context.WorkflowDefinitions
                .Where(w => w.Name == definition.Name && w.Id != definition.Id && w.IsActive)
                .ToListAsync();

            foreach (var other in activeOthers)
                other.IsActive = false;
        }

        definition.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDefinitionAsync(Guid id)
    {
        var definition = await _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (definition == null)
            return false;

        // Kiểm tra xem đã có bước duyệt runtime tham chiếu chưa
        var hasApprovalSteps = await _context.ApprovalSteps.AnyAsync(a => a.WorkflowDefinitionId == id);
        if (hasApprovalSteps)
        {
            throw new InvalidOperationException("Không thể xóa quy trình này vì đã có hợp đồng/bước phê duyệt tham chiếu trong lịch sử. Bạn có thể tắt trạng thái hoạt động (Deactivate) thay vì xóa.");
        }

        _context.WorkflowDefinitions.Remove(definition);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ResolveWorkflowResponse> ResolveWorkflowForContractAsync(decimal contractValue)
    {
        var activeWorkflows = await _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
            .Where(w => w.IsActive)
            .AsNoTracking()
            .ToListAsync();

        if (!activeWorkflows.Any())
        {
            return new ResolveWorkflowResponse
            {
                IsMatched = false,
                Message = "Hệ thống chưa cấu hình bất kỳ luồng duyệt nào đang hoạt động (Active)."
            };
        }

        // Ưu tiên 1: Luồng có biểu thức điều kiện cụ thể khớp với giá trị
        var conditionalWorkflows = activeWorkflows
            .Where(w => !string.IsNullOrWhiteSpace(w.ConditionExpression))
            .ToList();

        foreach (var wf in conditionalWorkflows)
        {
            if (_evaluator.Evaluate(wf.ConditionExpression, contractValue))
            {
                return new ResolveWorkflowResponse
                {
                    IsMatched = true,
                    Message = $"Khớp luồng duyệt có điều kiện: '{wf.Name}' (v{wf.Version}, điều kiện: '{wf.ConditionExpression}') cho giá trị hợp đồng {contractValue:N0} VNĐ.",
                    Workflow = MapToDto(wf)
                };
            }
        }

        // Ưu tiên 2: Luồng mặc định (không có biểu thức điều kiện)
        var defaultWorkflow = activeWorkflows.FirstOrDefault(w => string.IsNullOrWhiteSpace(w.ConditionExpression));
        if (defaultWorkflow != null)
        {
            return new ResolveWorkflowResponse
            {
                IsMatched = true,
                Message = $"Áp dụng luồng duyệt mặc định: '{defaultWorkflow.Name}' (v{defaultWorkflow.Version}) cho giá trị hợp đồng {contractValue:N0} VNĐ.",
                Workflow = MapToDto(defaultWorkflow)
            };
        }

        return new ResolveWorkflowResponse
        {
            IsMatched = false,
            Message = $"Không tìm thấy luồng duyệt nào thỏa mãn giá trị hợp đồng {contractValue:N0} VNĐ (các luồng duyệt hiện tại không khớp và không có luồng mặc định)."
        };
    }

    public EvaluateConditionResponse EvaluateCondition(string expression, decimal contractValue)
    {
        var isValid = _evaluator.TryValidate(expression, out var errorMessage);
        var isSatisfied = isValid && _evaluator.Evaluate(expression, contractValue);

        return new EvaluateConditionResponse
        {
            IsValidSyntax = isValid,
            ErrorMessage = errorMessage,
            IsSatisfied = isSatisfied
        };
    }

    // NEW: Add a step to an existing workflow definition
    public async Task<WorkflowStepDto> AddStepAsync(Guid workflowDefinitionId, CreateWorkflowStepRequest request)
    {
        // Validate the workflow definition exists
        var definition = await _context.WorkflowDefinitions
            .Include(w => w.WorkflowSteps)
            .FirstOrDefaultAsync(w => w.Id == workflowDefinitionId);

        if (definition == null)
            throw new KeyNotFoundException($"Không tìm thấy WorkflowDefinition có Id: {workflowDefinitionId}");

        // Validate step request (reuse validation logic)
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.StepOrder <= 0)
            throw new ArgumentException("Thứ tự bước duyệt (StepOrder) phải lớn hơn 0.", nameof(request.StepOrder));

        // Check for duplicate StepOrder within the same workflow
        if (definition.WorkflowSteps.Any(s => s.StepOrder == request.StepOrder))
            throw new InvalidOperationException($"Thứ tự bước {request.StepOrder} đã tồn tại trong luồng duyệt này.");

        // Create and add the step
        var step = new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = definition.Id,
            StepOrder = request.StepOrder,
            ApproverRole = request.ApproverRole,
            IsRequired = request.IsRequired,
            MinimumAmount = request.MinimumAmount
        };

        definition.WorkflowSteps.Add(step);
        await _context.SaveChangesAsync();

        return MapToStepDto(step);
    }

    private static void ValidateSteps(List<CreateWorkflowStepRequest>? steps)
    {
        if (steps == null || !steps.Any())
            throw new ArgumentException("Luồng duyệt phải có ít nhất một bước phê duyệt (Step).");

        if (steps.Any(s => s.StepOrder <= 0))
            throw new ArgumentException("Thứ tự bước duyệt (StepOrder) phải lớn hơn 0.");

        var distinctOrders = steps.Select(s => s.StepOrder).Distinct().Count();
        if (distinctOrders != steps.Count)
            throw new ArgumentException("Thứ tự bước duyệt (StepOrder) trong một luồng duyệt không được trùng lặp.");
    }

    private static WorkflowDefinitionDto MapToDto(WorkflowDefinition entity)
    {
        return new WorkflowDefinitionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ConditionExpression = entity.ConditionExpression,
            Version = entity.Version,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            Steps = entity.WorkflowSteps.Select(s => new WorkflowStepDto
            {
                Id = s.Id,
                WorkflowDefinitionId = s.WorkflowDefinitionId,
                StepOrder = s.StepOrder,
                ApproverRole = s.ApproverRole,
                IsRequired = s.IsRequired,
                MinimumAmount = s.MinimumAmount
            }).OrderBy(s => s.StepOrder).ToList()
        };
    }

    private static WorkflowStepDto MapToStepDto(WorkflowStep step)
    {
        return new WorkflowStepDto
        {
            Id = step.Id,
            WorkflowDefinitionId = step.WorkflowDefinitionId,
            StepOrder = step.StepOrder,
            ApproverRole = step.ApproverRole,
            IsRequired = step.IsRequired,
            MinimumAmount = step.MinimumAmount
        };
    }
}