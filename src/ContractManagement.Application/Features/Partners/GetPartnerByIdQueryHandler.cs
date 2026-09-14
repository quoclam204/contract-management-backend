using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Partners;
using ContractManagement.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Partners
{
    public class GetPartnerByIdQueryHandler : IRequestHandler<GetPartnerByIdQuery, PartnerDto?>
    {
        private readonly IPartnerDbContext _context;

        public GetPartnerByIdQueryHandler(IPartnerDbContext context)
        {
            _context = context;
        }

        public async Task<PartnerDto?> Handle(GetPartnerByIdQuery request, CancellationToken cancellationToken)
        {
            var partner = await _context.Partners
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (partner == null)
            {
                return null;
            }

            return new PartnerDto
            {
                Id = partner.Id,
                Name = partner.Name,
                TaxCode = partner.TaxCode,
                Representative = partner.Representative,
                ContactEmail = partner.ContactEmail,
                Address = partner.Address,
                CreatedAt = partner.CreatedAt
            };
        }
    }
}