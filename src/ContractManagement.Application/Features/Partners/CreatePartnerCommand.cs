using System;
using MediatR;

namespace ContractManagement.Application.Features.Partners
{
    public class CreatePartnerCommand : IRequest<PartnerDto>
    {
        public string Name { get; set; } = default!;
        public string TaxCode { get; set; } = default!;
        public string Representative { get; set; } = default!;
        public string ContactEmail { get; set; } = default!;
        public string Address { get; set; } = default!;
    }
}