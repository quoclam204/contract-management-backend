using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Partners;
using ContractManagement.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Partners
{
    public class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, PartnerDto>
    {
        private readonly IPartnerDbContext _context;

        public CreatePartnerCommandHandler(IPartnerDbContext context)
        {
            _context = context;
        }

        public async Task<PartnerDto> Handle(CreatePartnerCommand request, CancellationToken cancellationToken)
        {
            var partner = new Partner(
                Guid.NewGuid(),
                request.Name,
                request.TaxCode,
                request.Representative,
                request.ContactEmail,
                request.Address,
                DateTime.UtcNow
            );

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync(cancellationToken);

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