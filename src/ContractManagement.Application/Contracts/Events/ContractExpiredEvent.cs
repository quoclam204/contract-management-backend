using ContractManagement.Domain.Contracts.Entities;
using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event raised when a contract is expired.
/// </summary>
public record ContractExpiredEvent(Guid ContractId) : INotification;
