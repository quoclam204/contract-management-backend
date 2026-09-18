using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Attachments
{
    public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
    {
        private readonly IAttachmentDbContext _context;
        private readonly IStorageService _storageService;
        private readonly ICurrentUserService _currentUserService;

        private static readonly Guid FallbackUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public UploadAttachmentCommandHandler(
            IAttachmentDbContext context,
            IStorageService storageService,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _storageService = storageService;
            _currentUserService = currentUserService;
        }

        public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);
            if (contract == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy hợp đồng với Id: {request.ContractId}");
            }

            var maxVersion = await _context.Attachments
                .Where(a => a.ContractId == request.ContractId)
                .Select(a => (int?)a.Version)
                .MaxAsync(cancellationToken);

            var nextVersion = (maxVersion ?? 0) + 1;

            var uploadedBy = (request.UploadedBy.HasValue && request.UploadedBy.Value != Guid.Empty)
                ? request.UploadedBy.Value
                : (_currentUserService.UserId ?? FallbackUserId);

            long? fileSize = null;
            if (request.FileStream.CanSeek)
            {
                fileSize = request.FileStream.Length;
            }

            var fileUrl = await _storageService.SaveFileAsync(
                request.ContractId,
                nextVersion,
                request.FileName,
                request.FileStream,
                cancellationToken);

            var attachment = new Attachment(
                Guid.NewGuid(),
                request.ContractId,
                request.FileName,
                nextVersion,
                fileUrl,
                uploadedBy,
                DateTime.UtcNow
            );

            _context.Attachments.Add(attachment);
            contract.FileUrl = fileUrl;

            await _context.SaveChangesAsync(cancellationToken);

            return new AttachmentDto
            {
                Id = attachment.Id,
                ContractId = attachment.ContractId,
                FileName = attachment.FileName,
                Version = attachment.Version,
                FileUrl = attachment.FileUrl,
                UploadedBy = attachment.UploadedBy,
                UploadedAt = attachment.UploadedAt,
                FileSize = fileSize
            };
        }
    }
}
