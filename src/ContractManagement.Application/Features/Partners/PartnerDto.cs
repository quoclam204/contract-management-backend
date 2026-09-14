using System;

namespace ContractManagement.Application.Features.Partners
{
    public class PartnerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? TaxCode { get; set; }
        public string? Representative { get; set; }
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}