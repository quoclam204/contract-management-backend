using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Partners;

public class GetPartnersQueryHandler : IRequestHandler<GetPartnersQuery, PagedResult<PartnerDto>>
{
    private readonly IPartnerDbContext _context;

    public GetPartnersQueryHandler(IPartnerDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PartnerDto>> Handle(GetPartnersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Partners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (p.TaxCode != null && p.TaxCode.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PartnerDto
            {
                Id = p.Id,
                Name = p.Name,
                TaxCode = p.TaxCode,
                Representative = p.Representative,
                ContactEmail = p.ContactEmail,
                Address = p.Address,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PartnerDto>(items, totalCount, pageNumber, pageSize);
    }
}