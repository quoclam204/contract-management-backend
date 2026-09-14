using System;
using MediatR;

namespace ContractManagement.Application.Features.Attachments
{
    public class GetAttachmentForDownloadQuery : IRequest<AttachmentDownloadDto?>
    {
        public Guid AttachmentId { get; set; }

        public GetAttachmentForDownloadQuery(Guid attachmentId)
        {
            AttachmentId = attachmentId;
        }

        public GetAttachmentForDownloadQuery() { }
    }
}
