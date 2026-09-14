using System;
using System.IO;
using MediatR;

namespace ContractManagement.Application.Features.Attachments
{
    public class UploadAttachmentCommand : IRequest<AttachmentDto>
    {
        public Guid ContractId { get; set; }
        public string FileName { get; set; } = default!;
        public Stream FileStream { get; set; } = default!;
        public Guid? UploadedBy { get; set; }
    }
}
