using ContractManagement.Domain.Contracts.Entities;
using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event raised when a contract is terminated.
/// </summary>
public record ContractTerminatedEvent(Guid ContractId) : INotification;
