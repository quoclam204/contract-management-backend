using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Partners;
using ContractManagement.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Partners
{
    public class DeletePartnerCommandHandler : IRequestHandler<DeletePartnerRequest, bool>
    {
        private readonly IPartnerDbContext _context;

        public DeletePartnerCommandHandler(IPartnerDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeletePartnerRequest request, CancellationToken cancellationToken)
        {
            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (partner == null)
            {
                return false;
            }

            _context.Partners.Remove(partner);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}