using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Attachments
{
    public class GetAttachmentForDownloadQueryHandler : IRequestHandler<GetAttachmentForDownloadQuery, AttachmentDownloadDto?>
    {
        private readonly IAttachmentDbContext _context;
        private readonly IStorageService _storageService;

        public GetAttachmentForDownloadQueryHandler(
            IAttachmentDbContext context,
            IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<AttachmentDownloadDto?> Handle(GetAttachmentForDownloadQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _context.Attachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

            if (attachment == null)
            {
                return null;
            }

            var fileStream = await _storageService.GetFileAsync(attachment.FileUrl, cancellationToken);
            var contentType = GetContentType(attachment.FileName);

            return new AttachmentDownloadDto
            {
                FileStream = fileStream,
                FileName = attachment.FileName,
                ContentType = contentType
            };
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".txt" => "text/plain",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}
