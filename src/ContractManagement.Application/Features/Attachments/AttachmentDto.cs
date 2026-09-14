using System;

namespace ContractManagement.Application.Features.Attachments
{
    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public string FileName { get; set; } = default!;
        public int Version { get; set; }
        public string FileUrl { get; set; } = default!;
        public Guid UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
