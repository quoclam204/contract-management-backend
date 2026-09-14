using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Workflow.DTOs;
using ContractManagement.Application.Workflow.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Application.Workflow.Handlers;

/// <summary>
/// Handles the ContractSubmittedEvent to initiate the approval process.
/// </summary>
public class ContractSubmittedEventHandler : INotificationHandler<ContractSubmittedEvent>
{
    private readonly IWorkflowService _workflowService;
    private readonly IApprovalService _approvalService;
    private readonly IMediator _mediator;
    private readonly ILogger<ContractSubmittedEventHandler> _logger;

    public ContractSubmittedEventHandler(
        IWorkflowService workflowService,
        IApprovalService approvalService,
        IMediator mediator,
        ILogger<ContractSubmittedEventHandler> logger)
    {
        _workflowService = workflowService;
        _approvalService = approvalService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(ContractSubmittedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ContractSubmittedEvent for ContractId: {ContractId}", notification.ContractId);

        Guid? workflowDefinitionId = notification.WorkflowDefinitionId;

        // If the template version has a direct workflow definition, use it.
        // Otherwise, resolve the workflow based on the contract value.
        if (!workflowDefinitionId.HasValue)
        {
            _logger.LogInformation("No direct workflow definition found for contract. Resolving workflow based on contract value.");
            var workflowResolutionResult = await _workflowService.ResolveWorkflowForContractAsync(notification.ContractValue);
            if (!workflowResolutionResult.IsMatched || workflowResolutionResult.Workflow == null)
            {
                _logger.LogWarning("No workflow found for contract value: {ContractValue}", notification.ContractValue);
                throw new InvalidOperationException(workflowResolutionResult.Message ?? $"Không tìm thấy luồng duyệt phù hợp cho giá trị hợp đồng: {notification.ContractValue:N0}");
            }
            workflowDefinitionId = workflowResolutionResult.Workflow.Id;
        }

        // Submit the contract for approval using the resolved workflow definition.
        var approvalRequest = new SubmitContractApprovalRequest
        {
            ContractId = notification.ContractId,
            ContractValue = notification.ContractValue,
            WorkflowDefinitionId = workflowDefinitionId,
            ApproverId = null // Let the system determine the approver or use a default.
        };

        await _approvalService.SubmitForApprovalAsync(approvalRequest);

        _logger.LogInformation("Successfully submitted contract {ContractId} for approval with workflow {WorkflowId}",
            notification.ContractId, workflowDefinitionId);
    }
}