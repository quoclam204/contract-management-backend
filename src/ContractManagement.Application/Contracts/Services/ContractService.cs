using ContractManagement.Application.Contracts.DTOs;
using ContractManagement.Application.Contracts.Events;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Domain.Contracts.Entities;
using ContractManagement.Domain.Contracts.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contracts.Services;

/// <summary>
/// Service for managing contracts.
/// </summary>
public class ContractService : IContractService
{
    private readonly IContractDbContext _context;
    private readonly IMediator _mediator;

    public ContractService(IContractDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<ContractDto> CreateContractAsync(CreateContractRequest request)
    {
        var contract = new ContractManagement.Domain.Contracts.Entities.Contract
        {
            ContractNumber = request.ContractNumber,
            ContractTypeId = request.ContractTypeId,
            TemplateVersionUsedId = request.TemplateVersionUsedId,
            PartnerId = request.PartnerId,
            OwnerId = request.OwnerId,
            Title = request.Title,
            Value = request.Value,
            SignedDate = request.SignedDate,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            FileUrl = request.FileUrl,
            ParentContractId = request.ParentContractId
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        return new ContractDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            ContractTypeId = contract.ContractTypeId,
            TemplateVersionUsedId = contract.TemplateVersionUsedId,
            PartnerId = contract.PartnerId,
            OwnerId = contract.OwnerId,
            Title = contract.Title,
            Value = contract.Value,
            SignedDate = contract.SignedDate,
            EffectiveDate = contract.EffectiveDate,
            ExpiryDate = contract.ExpiryDate,
            Status = contract.Status,
            FileUrl = contract.FileUrl,
            ParentContractId = contract.ParentContractId,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt
        };
    }

    public async Task<ContractDto?> GetContractByIdAsync(Guid id)
    {
        var contract = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.ContractType)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            return null;

        return new ContractDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            ContractTypeId = contract.ContractTypeId,
            ContractTypeName = contract.ContractType?.Name,
            TemplateVersionUsedId = contract.TemplateVersionUsedId,
            PartnerId = contract.PartnerId,
            OwnerId = contract.OwnerId,
            Title = contract.Title,
            Value = contract.Value,
            SignedDate = contract.SignedDate,
            EffectiveDate = contract.EffectiveDate,
            ExpiryDate = contract.ExpiryDate,
            Status = contract.Status,
            FileUrl = contract.FileUrl,
            ParentContractId = contract.ParentContractId,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt
        };
    }

    public async Task<List<ContractDto>> GetContractsAsync()
    {
        return await _context.Contracts
            .AsNoTracking()
            .Include(c => c.ContractType)
            .Select(c => new ContractDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                ContractTypeId = c.ContractTypeId,
                ContractTypeName = c.ContractType != null ? c.ContractType.Name : null,
                TemplateVersionUsedId = c.TemplateVersionUsedId,
                PartnerId = c.PartnerId,
                OwnerId = c.OwnerId,
                Title = c.Title,
                Value = c.Value,
                SignedDate = c.SignedDate,
                EffectiveDate = c.EffectiveDate,
                ExpiryDate = c.ExpiryDate,
                Status = c.Status,
                FileUrl = c.FileUrl,
                ParentContractId = c.ParentContractId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<ContractDto> UpdateContractAsync(Guid id, UpdateContractRequest request)
    {
        var contract = await _context.Contracts.FindAsync(id);

        if (contract == null)
            throw new KeyNotFoundException($"Không tìm thấy hợp đồng với Id: {id}");

        contract.Update(
            title: request.Title,
            contractNumber: request.ContractNumber,
            contractTypeId: request.ContractTypeId,
            templateVersionUsedId: request.TemplateVersionUsedId,
            partnerId: request.PartnerId,
            ownerId: request.OwnerId,
            value: request.Value,
            signedDate: request.SignedDate,
            effectiveDate: request.EffectiveDate,
            expiryDate: request.ExpiryDate,
            fileUrl: request.FileUrl,
            parentContractId: request.ParentContractId
        );

        _context.Contracts.Update(contract);
        await _context.SaveChangesAsync();

        var contractType = await _context.ContractTypes.FindAsync(contract.ContractTypeId);
        string? typeName = contractType?.Name;

        return new ContractDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            ContractTypeId = contract.ContractTypeId,
            ContractTypeName = typeName,
            TemplateVersionUsedId = contract.TemplateVersionUsedId,
            PartnerId = contract.PartnerId,
            OwnerId = contract.OwnerId,
            Title = contract.Title,
            Value = contract.Value,
            SignedDate = contract.SignedDate,
            EffectiveDate = contract.EffectiveDate,
            ExpiryDate = contract.ExpiryDate,
            Status = contract.Status,
            FileUrl = contract.FileUrl,
            ParentContractId = contract.ParentContractId,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt
        };
    }

    public async Task<bool> DeleteContractAsync(Guid id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            return false;

        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SubmitContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain method validates Draft status and required data, then transitions to PendingApproval
        contract.Submit();

        // Persist change to database first
        await _context.SaveChangesAsync();

        // Get the template version to determine the workflow definition id (if any)
        var templateVersion = await _context.ContractTemplateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(tv => tv.Id == contract.TemplateVersionUsedId);

        Guid? workflowDefinitionId = templateVersion?.WorkflowDefinitionId;

        // Publish the event after successful persistence for the workflow module to handle
        await _mediator.Publish(new ContractSubmittedEvent(
            ContractId: contract.Id,
            ContractValue: contract.Value,
            WorkflowDefinitionId: workflowDefinitionId
        ));
    }

    public async Task ApproveContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain transition
        contract.Approve();

        await _context.SaveChangesAsync();
        await _mediator.Publish(new ContractApprovedEvent(contract.Id));
    }

    public async Task SignContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain transition
        contract.Sign();

        await _context.SaveChangesAsync();
        await _mediator.Publish(new ContractSignedEvent(contract.Id));
    }

    public async Task ActivateContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain transition
        contract.Activate();

        await _context.SaveChangesAsync();
        await _mediator.Publish(new ContractActivatedEvent(contract.Id));
    }

    public async Task ExpireContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain transition
        contract.Expire();

        await _context.SaveChangesAsync();
        await _mediator.Publish(new ContractExpiredEvent(contract.Id));
    }

    public async Task RenewContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain transition
        contract.Renew();

        await _context.SaveChangesAsync();
        await _mediator.Publish(new ContractRenewedEvent(contract.Id));
    }

    public async Task TerminateContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null)
            throw new KeyNotFoundException($"Contract with Id {contractId} not found.");

        // Domain transition
        contract.Terminate();

        await _context.SaveChangesAsync();
        await _mediator.Publish(new ContractTerminatedEvent(contract.Id));
    }
}
