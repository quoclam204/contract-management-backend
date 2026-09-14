using System;
using MediatR;

namespace ContractManagement.Application.Features.Partners
{
    public record DeletePartnerRequest(Guid Id) : IRequest<bool>;
}