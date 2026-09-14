using ContractManagement.Domain.Contracts.Entities;
using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event raised when a contract is created.
/// </summary>
public record ContractCreatedEvent(Guid ContractId) : INotification;
