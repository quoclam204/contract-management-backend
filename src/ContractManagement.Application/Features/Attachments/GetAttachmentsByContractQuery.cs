using System;
using System.Collections.Generic;
using MediatR;

namespace ContractManagement.Application.Features.Attachments
{
    public class GetAttachmentsByContractQuery : IRequest<List<AttachmentDto>>
    {
        public Guid ContractId { get; set; }

        public GetAttachmentsByContractQuery(Guid contractId)
        {
            ContractId = contractId;
        }

        public GetAttachmentsByContractQuery() { }
    }
}
