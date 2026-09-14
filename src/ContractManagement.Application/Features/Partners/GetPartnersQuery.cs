using ContractManagement.Application.Common.Models;
using MediatR;

namespace ContractManagement.Application.Features.Partners;

public record GetPartnersQuery : IRequest<PagedResult<PartnerDto>>
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }

    public string? SearchTerm { get; init; }
}