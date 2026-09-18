using System;
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
        private readonly IStorageService? _storageService;

        public GetAttachmentsByContractQueryHandler(IAttachmentDbContext context, IStorageService? storageService = null)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<List<AttachmentDto>> Handle(GetAttachmentsByContractQuery request, CancellationToken cancellationToken)
        {
            var attachments = await _context.Attachments
                .AsNoTracking()
                .Where(a => a.ContractId == request.ContractId)
                .OrderByDescending(a => a.Version)
                .ToListAsync(cancellationToken);

            var list = new List<AttachmentDto>();
            foreach (var a in attachments)
            {
                long? size = null;
                if (_storageService != null && !string.IsNullOrEmpty(a.FileUrl))
                {
                    try
                    {
                        using var stream = await _storageService.GetFileAsync(a.FileUrl, cancellationToken);
                        size = stream.Length;
                    }
                    catch
                    {
                        // File not on disk or missing
                    }
                }

                list.Add(new AttachmentDto
                {
                    Id = a.Id,
                    ContractId = a.ContractId,
                    FileName = a.FileName,
                    Version = a.Version,
                    FileUrl = a.FileUrl,
                    UploadedBy = a.UploadedBy,
                    UploadedAt = a.UploadedAt,
                    FileSize = size
                });
            }

            return list;
        }
    }
}
