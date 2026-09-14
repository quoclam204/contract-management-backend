using ContractManagement.Domain.Contracts.Entities;
using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event raised when a contract is activated.
/// </summary>
public record ContractActivatedEvent(Guid ContractId) : INotification;
