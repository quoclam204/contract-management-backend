using ContractManagement.Domain.Contracts.Entities;
using MediatR;

namespace ContractManagement.Application.Contracts.Events;

/// <summary>
/// Event raised when a contract is renewed.
/// </summary>
public record ContractRenewedEvent(Guid ContractId) : INotification;
