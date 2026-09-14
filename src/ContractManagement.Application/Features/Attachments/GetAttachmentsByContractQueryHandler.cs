using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Attachments
{
    public class GetAttachmentsByContractQueryHandler : IRequestHandler<GetAttachmentsByContractQuery, List<AttachmentDto>>
    {
        private readonly IAttachmentDbContext _context;

        public GetAttachmentsByContractQueryHandler(IAttachmentDbContext context)
        {
            _context = context;
        }

        public async Task<List<AttachmentDto>> Handle(GetAttachmentsByContractQuery request, CancellationToken cancellationToken)
        {
            return await _context.Attachments
                .AsNoTracking()
                .Where(a => a.ContractId == request.ContractId)
                .OrderByDescending(a => a.Version)
                .Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    ContractId = a.ContractId,
                    FileName = a.FileName,
                    Version = a.Version,
                    FileUrl = a.FileUrl,
                    UploadedBy = a.UploadedBy,
                    UploadedAt = a.UploadedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
