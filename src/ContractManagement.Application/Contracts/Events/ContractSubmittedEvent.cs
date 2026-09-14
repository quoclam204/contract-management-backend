using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event published when a contract is submitted for approval.
/// Workflow module handles this to initiate the approval process.
/// </summary>
/// <param name="ContractId">ID of the contract being submitted</param>
/// <param name="ContractValue">Value of the contract (used for workflow resolution when template has no direct workflow)</param>
/// <param name="WorkflowDefinitionId">Workflow ID from template version (if present); null requires resolution via contract value</param>
public record ContractSubmittedEvent(Guid ContractId, decimal ContractValue, Guid? WorkflowDefinitionId) : INotification;