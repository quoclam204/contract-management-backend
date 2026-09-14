using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Events;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain.Workflow.Entities;
using ContractManagement.Domain.Workflow.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Workflow.Services;

public class ApprovalService : IApprovalService
{
    private readonly IWorkflowDbContext _context;
    private readonly IWorkflowService _workflowService;
    private readonly IPublisher _publisher;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IWorkflowDbContext context,
        IWorkflowService workflowService,
        IPublisher publisher,
        ILogger<ApprovalService> logger)
    {
        _context = context;
        _workflowService = workflowService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ContractApprovalProgressDto> SubmitForApprovalAsync(SubmitContractApprovalRequest request)
    {
        if (request.ContractId == Guid.Empty)
            throw new ArgumentException("ContractId không hợp lệ.", nameof(request.ContractId));

        // 1. Kiểm tra xem hợp đồng đã có tiến trình phê duyệt đang dở dang (Pending) không
        var existingSteps = await _context.ApprovalSteps
            .Where(s => s.ContractId == request.ContractId)
            .ToListAsync();

        if (existingSteps.Any(s => s.Decision == ApprovalDecision.Pending) && !existingSteps.Any(s => s.Decision == ApprovalDecision.Rejected))
        {
            throw new InvalidOperationException("Hợp đồng này đang trong tiến trình phê duyệt (có bước đang ở trạng thái Chờ duyệt - Pending). Không thể đệ trình lại lúc này.");
        }

        // 2. Xác định WorkflowDefinition áp dụng
        WorkflowDefinition? workflow = null;
        if (request.WorkflowDefinitionId.HasValue && request.WorkflowDefinitionId.Value != Guid.Empty)
        {
            workflow = await _context.WorkflowDefinitions
                .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
                .FirstOrDefaultAsync(w => w.Id == request.WorkflowDefinitionId.Value && w.IsActive);

            if (workflow == null)
                throw new ArgumentException("Luồng duyệt được chỉ định không tồn tại hoặc đã ngừng hoạt động (Inactive).", nameof(request.WorkflowDefinitionId));
        }
        else
        {
            // Tự động phân tích theo giá trị hợp đồng
            var resolveResult = await _workflowService.ResolveWorkflowForContractAsync(request.ContractValue);
            if (!resolveResult.IsMatched || resolveResult.Workflow == null)
            {
                throw new InvalidOperationException(resolveResult.Message);
            }

            workflow = await _context.WorkflowDefinitions
                .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
                .FirstOrDefaultAsync(w => w.Id == resolveResult.Workflow.Id);

            if (workflow == null)
                throw new InvalidOperationException("Không tải được thông tin luồng duyệt vừa xác định.");
        }

        if (!workflow.WorkflowSteps.Any())
            throw new InvalidOperationException($"Luồng duyệt '{workflow.Name}' chưa có bất kỳ bước phê duyệt nào.");

        // 3. Nếu trước đó hợp đồng từng bị Reject và nay submit lại, dọn dẹp các bước cũ để mở chu kỳ duyệt mới
        if (existingSteps.Any())
        {
            _context.ApprovalSteps.RemoveRange(existingSteps);
            await _context.SaveChangesAsync();
        }

        // 4. Lấy một ApproverId mặc định nếu request không truyền
        var defaultApproverId = request.ApproverId ?? Guid.Empty;
        if (defaultApproverId == Guid.Empty)
        {
            defaultApproverId = await _context.GetDefaultApproverIdAsync();
        }

        // 5. Khởi tạo danh sách APPROVAL_STEPS snapshot theo WorkflowDefinition
        var newSteps = new List<ApprovalStep>();
        foreach (var ws in workflow.WorkflowSteps.OrderBy(s => s.StepOrder))
        {
            var step = new ApprovalStep
            {
                Id = Guid.NewGuid(),
                ContractId = request.ContractId,
                WorkflowDefinitionId = workflow.Id,
                StepOrder = ws.StepOrder,
                Decision = ApprovalDecision.Pending,
                ApproverId = defaultApproverId,
                Comment = null,
                DecidedAt = null,
                CreatedAt = DateTime.UtcNow
            };
            newSteps.Add(step);
        }

        _context.ApprovalSteps.AddRange(newSteps);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Hợp đồng {ContractId} đã được Submit vào luồng duyệt '{WorkflowName}' (v{Version}) với {TotalSteps} bước.",
            request.ContractId, workflow.Name, workflow.Version, newSteps.Count);

        return await BuildProgressDtoAsync(request.ContractId, workflow, newSteps);
    }

    public async Task<ContractApprovalProgressDto> ProcessDecisionAsync(ProcessApprovalDecisionRequest request)
    {
        var step = await _context.ApprovalSteps
            .Include(s => s.WorkflowDefinition)
            .ThenInclude(w => w.WorkflowSteps)
            .FirstOrDefaultAsync(s => s.Id == request.ApprovalStepId);

        if (step == null)
            throw new KeyNotFoundException($"Không tìm thấy bước duyệt có Id: {request.ApprovalStepId}");

        if (step.Decision != ApprovalDecision.Pending)
        {
            throw new InvalidOperationException($"Bước duyệt số {step.StepOrder} đã có quyết định ({step.Decision}) vào lúc {step.DecidedAt:dd/MM/yyyy HH:mm:ss}. Không thể duyệt lại.");
        }

        if (request.Decision == ApprovalDecision.Pending)
            throw new ArgumentException("Quyết định phê duyệt phải là Approved (1) hoặc Rejected (2).", nameof(request.Decision));

        if (request.Decision == ApprovalDecision.Rejected && string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("Vui lòng nhập lý do từ chối phê duyệt (Comment).", nameof(request.Comment));

        // Kiểm tra tính tuần tự: tất cả các bước trước đó (StepOrder < hiện tại) PHẢI đã được Approved
        var allSteps = await _context.ApprovalSteps
            .Where(s => s.ContractId == step.ContractId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        var priorUnapproved = allSteps.FirstOrDefault(s => s.StepOrder < step.StepOrder && s.Decision != ApprovalDecision.Approved);
        if (priorUnapproved != null)
        {
            throw new InvalidOperationException($"Không thể duyệt bước {step.StepOrder} vì bước {priorUnapproved.StepOrder} chưa được phê duyệt.");
        }

        // Cập nhật quyết định của bước hiện tại
        step.Decision = request.Decision;
        step.Comment = request.Comment?.Trim();
        step.DecidedAt = DateTime.UtcNow;
        if (request.ApproverId != Guid.Empty)
            step.ApproverId = request.ApproverId;

        await _context.SaveChangesAsync();

        // Xử lý sự kiện MediatR
        if (request.Decision == ApprovalDecision.Rejected)
        {
            _logger.LogWarning("Hợp đồng {ContractId} bị từ chối ở bước {StepOrder}. Lý do: {Reason}",
                step.ContractId, step.StepOrder, request.Comment);

            // Bắn sự kiện Rejected qua MediatR (Người 2 sẽ bắt event này để đổi trạng thái hợp đồng)
            await _publisher.Publish(new WorkflowRejectedEvent(step.ContractId, request.Comment));
        }
        else if (request.Decision == ApprovalDecision.Approved)
        {
            // Kiểm tra xem tất cả các bước đã được duyệt hết chưa
            var remainingPending = allSteps.Any(s => s.Id != step.Id && s.Decision != ApprovalDecision.Approved);

            if (!remainingPending)
            {
                _logger.LogInformation("Hợp đồng {ContractId} đã hoàn tất phê duyệt qua tất cả {TotalSteps} bước! Bắn sự kiện WorkflowApprovedEvent.",
                    step.ContractId, allSteps.Count);

                // Bắn sự kiện Approved qua MediatR (Người 2 sẽ bắt event này để đổi trạng thái hợp đồng)
                await _publisher.Publish(new WorkflowApprovedEvent(step.ContractId));
            }
            else
            {
                _logger.LogInformation("Hợp đồng {ContractId} đã duyệt thành công bước {StepOrder}. Đang chờ các bước tiếp theo.",
                    step.ContractId, step.StepOrder);
            }
        }

        return await BuildProgressDtoAsync(step.ContractId, step.WorkflowDefinition, allSteps);
    }

    public async Task<ContractApprovalProgressDto?> GetProgressByContractIdAsync(Guid contractId)
    {
        var steps = await _context.ApprovalSteps
            .Include(s => s.WorkflowDefinition)
            .ThenInclude(w => w.WorkflowSteps)
            .Where(s => s.ContractId == contractId)
            .OrderBy(s => s.StepOrder)
            .AsNoTracking()
            .ToListAsync();

        if (!steps.Any())
            return null;

        var workflow = steps.First().WorkflowDefinition;
        return await BuildProgressDtoAsync(contractId, workflow, steps);
    }

    public async Task<List<PendingApprovalItemDto>> GetPendingApprovalsAsync(Guid? approverId = null)
    {
        var query = _context.ApprovalSteps
            .Include(s => s.WorkflowDefinition)
            .ThenInclude(w => w.WorkflowSteps)
            .Where(s => s.Decision == ApprovalDecision.Pending)
            .AsNoTracking();

        if (approverId.HasValue && approverId.Value != Guid.Empty)
            query = query.Where(s => s.ApproverId == approverId.Value);

        var list = await query
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var result = new List<PendingApprovalItemDto>();
        foreach (var item in list)
        {
            var ws = item.WorkflowDefinition.WorkflowSteps.FirstOrDefault(s => s.StepOrder == item.StepOrder);
            var role = ws?.ApproverRole ?? ApproverRole.Approver;

            result.Add(new PendingApprovalItemDto
            {
                ApprovalStepId = item.Id,
                ContractId = item.ContractId,
                WorkflowDefinitionId = item.WorkflowDefinitionId,
                WorkflowName = item.WorkflowDefinition.Name,
                StepOrder = item.StepOrder,
                ApproverRole = role,
                ApproverId = item.ApproverId,
                CreatedAt = item.CreatedAt
            });
        }

        return result;
    }

    private Task<ContractApprovalProgressDto> BuildProgressDtoAsync(
        Guid contractId,
        WorkflowDefinition workflow,
        List<ApprovalStep> steps)
    {
        var overallStatus = "Pending";
        if (steps.Any(s => s.Decision == ApprovalDecision.Rejected))
            overallStatus = "Rejected";
        else if (steps.All(s => s.Decision == ApprovalDecision.Approved))
            overallStatus = "Approved";

        var currentPending = steps.FirstOrDefault(s => s.Decision == ApprovalDecision.Pending);

        var stepDetails = new List<ApprovalStepDetailDto>();
        foreach (var s in steps.OrderBy(s => s.StepOrder))
        {
            var ws = workflow.WorkflowSteps.FirstOrDefault(w => w.StepOrder == s.StepOrder);
            var role = ws?.ApproverRole ?? ApproverRole.Approver;

            stepDetails.Add(new ApprovalStepDetailDto
            {
                Id = s.Id,
                ContractId = s.ContractId,
                WorkflowDefinitionId = s.WorkflowDefinitionId,
                StepOrder = s.StepOrder,
                ApproverRole = role,
                ApproverId = s.ApproverId,
                Decision = s.Decision,
                Comment = s.Comment,
                DecidedAt = s.DecidedAt,
                CreatedAt = s.CreatedAt
            });
        }

        var dto = new ContractApprovalProgressDto
        {
            ContractId = contractId,
            WorkflowDefinitionId = workflow.Id,
            WorkflowName = workflow.Name,
            WorkflowVersion = workflow.Version,
            OverallStatus = overallStatus,
            CurrentPendingStepOrder = currentPending?.StepOrder,
            TotalSteps = steps.Count,
            ApprovedStepsCount = steps.Count(s => s.Decision == ApprovalDecision.Approved),
            Steps = stepDetails
        };

        return Task.FromResult(dto);
    }
}
