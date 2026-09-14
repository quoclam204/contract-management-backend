using System.IO;

namespace ContractManagement.Application.Features.Attachments
{
    public class AttachmentDownloadDto
    {
        public Stream FileStream { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
