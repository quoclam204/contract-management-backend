using ContractManagement.Domain.Contracts.Entities;
using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event raised when a contract is approved.
/// </summary>
public record ContractApprovedEvent(Guid ContractId) : INotification;
