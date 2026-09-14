using System;

namespace ContractManagement.Domain
{
    public class Partner
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = default!;
        public string? TaxCode { get; private set; }
        public string? Representative { get; private set; }
        public string? ContactEmail { get; private set; }
        public string? Address { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Constructor for creating a new partner
        public Partner(Guid id, string name, string? taxCode, string? representative, string? contactEmail, string? address, DateTime createdAt)
        {
            Id = id;
            Name = name;
            TaxCode = taxCode;
            Representative = representative;
            ContactEmail = contactEmail;
            Address = address;
            CreatedAt = createdAt;
        }

        // Constructor for EF Core (parameterless)
        protected Partner() { }

        // Methods to update partner properties (if needed)
        public void UpdateDetails(string name, string? taxCode, string? representative, string? contactEmail, string? address)
        {
            Name = name;
            TaxCode = taxCode;
            Representative = representative;
            ContactEmail = contactEmail;
            Address = address;
        }
    }
}