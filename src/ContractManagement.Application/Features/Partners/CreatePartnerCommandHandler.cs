using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Partners;
using ContractManagement.Domain;
using FluentValidation;
using FluentValidation.Results;
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
            var trimmedName = request.Name?.Trim() ?? string.Empty;
            var trimmedTaxCode = request.TaxCode?.Trim();

            // Kiểm tra xem Mã số thuế (TaxCode) hoặc Tên (Name) đã tồn tại trong CSDL hay chưa
            var nameExists = !string.IsNullOrWhiteSpace(trimmedName) && await _context.Partners
                .AnyAsync(p => p.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

            var taxCodeExists = !string.IsNullOrWhiteSpace(trimmedTaxCode) && await _context.Partners
                .AnyAsync(p => p.TaxCode == trimmedTaxCode, cancellationToken);

            if (nameExists || taxCodeExists)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(
                        taxCodeExists ? nameof(request.TaxCode) : nameof(request.Name),
                        "Mã số thuế hoặc Tên đối tác đã tồn tại trong hệ thống."
                    )
                });
            }

            var partner = new Partner(
                Guid.NewGuid(),
                trimmedName,
                trimmedTaxCode,
                request.Representative?.Trim(),
                request.ContactEmail?.Trim(),
                request.Address?.Trim(),
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