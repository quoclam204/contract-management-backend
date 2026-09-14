using System;
using MediatR;

namespace ContractManagement.Application.Features.Partners
{
    public record GetPartnerByIdQuery(Guid Id) : IRequest<PartnerDto>;
}