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

        // Seeded User Id cho Người 3 trong database
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
            // 1. Kiểm tra tồn tại của hợp đồng
            var contractExists = await _context.Contracts.AnyAsync(c => c.Id == request.ContractId, cancellationToken);
            if (!contractExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy hợp đồng với Id: {request.ContractId}");
            }

            // 2. Logic Versioning:
            // Kiểm tra hợp đồng đã có file đính kèm chưa. Nếu chưa -> Version = 1; nếu đã có -> lấy Max(Version) + 1
            var maxVersion = await _context.Attachments
                .Where(a => a.ContractId == request.ContractId)
                .Select(a => (int?)a.Version)
                .MaxAsync(cancellationToken);

            var nextVersion = (maxVersion ?? 0) + 1;

            // 3. Xác định UploadedBy từ request hoặc CurrentUserService
            var uploadedBy = (request.UploadedBy.HasValue && request.UploadedBy.Value != Guid.Empty)
                ? request.UploadedBy.Value
                : (_currentUserService.UserId ?? FallbackUserId);

            // 4. Lưu file vật lý qua IStorageService
            var fileUrl = await _storageService.SaveFileAsync(
                request.ContractId,
                nextVersion,
                request.FileName,
                request.FileStream,
                cancellationToken);

            // 5. Lưu bản ghi Attachment mới vào DB
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
            await _context.SaveChangesAsync(cancellationToken);

            return new AttachmentDto
            {
                Id = attachment.Id,
                ContractId = attachment.ContractId,
                FileName = attachment.FileName,
                Version = attachment.Version,
                FileUrl = attachment.FileUrl,
                UploadedBy = attachment.UploadedBy,
                UploadedAt = attachment.UploadedAt
            };
        }
    }
}
