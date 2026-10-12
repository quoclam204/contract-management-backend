using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Application.Features.Partners;
using ContractManagement.Application.Workflow.Interfaces;
using ContractManagement.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Features.Partners
{
    public class DeletePartnerCommandHandler : IRequestHandler<DeletePartnerRequest, bool>
    {
        private readonly IPartnerDbContext _context;
        private readonly IContractDbContext _contractContext;
        private readonly IWorkflowDbContext _workflowContext;

        public DeletePartnerCommandHandler(
            IPartnerDbContext context,
            IContractDbContext contractContext,
            IWorkflowDbContext workflowContext)
        {
            _context = context;
            _contractContext = contractContext;
            _workflowContext = workflowContext;
        }

        public async Task<bool> Handle(DeletePartnerRequest request, CancellationToken cancellationToken)
        {
            var partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (partner == null)
            {
                return false;
            }

            // Kiểm tra các hợp đồng đang liên kết với đối tác
            var linkedContractsCount = await _contractContext.Contracts
                .CountAsync(c => c.PartnerId == request.Id, cancellationToken);

            if (linkedContractsCount > 0)
            {
                throw new InvalidOperationException(
                    $"Không thể xóa đối tác \"{partner.Name}\" vì đang có {linkedContractsCount} hợp đồng liên kết trong hệ thống. Vui lòng xử lý hoặc chuyển đổi các hợp đồng liên quan trước.");
            }

            // Kiểm tra các chữ ký điện tử liên quan đến đối tác
            var linkedSignaturesCount = await _workflowContext.Signatures
                .CountAsync(s => s.PartnerSignerId == request.Id, cancellationToken);

            if (linkedSignaturesCount > 0)
            {
                throw new InvalidOperationException(
                    $"Không thể xóa đối tác \"{partner.Name}\" vì đang có {linkedSignaturesCount} chữ ký điện tử liên kết trong hệ thống.");
            }

            _context.Partners.Remove(partner);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}