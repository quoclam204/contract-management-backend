using ContractManagement.Application.Contract.DTOs;
using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Domain.Contract.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contract.Services;

public class ContractTemplateVersionService : IContractTemplateVersionService
{
    private readonly IContractManagementDbContext _context;

    public ContractTemplateVersionService(IContractManagementDbContext context)
    {
        _context = context;
    }

    public async Task<List<ContractTemplateVersionDto>> GetAllAsync()
    {
        return await _context.ContractTemplateVersions
            .Select(ctv => new ContractTemplateVersionDto
            {
                Id = ctv.Id,
                ContractTypeId = ctv.ContractTypeId,
                Version = ctv.Version,
                TemplateFileUrl = ctv.TemplateFileUrl,
                ContentJson = ctv.ContentJson,
                WorkflowDefinitionId = ctv.WorkflowDefinitionId,
                IsActive = ctv.IsActive,
                CreatedBy = ctv.CreatedBy,
                CreatedAt = ctv.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ContractTemplateVersionDto?> GetByIdAsync(Guid id)
    {
        var contractTemplateVersion = await _context.ContractTemplateVersions.FindAsync(id);
        if (contractTemplateVersion == null)
            return null;

        return new ContractTemplateVersionDto
        {
            Id = contractTemplateVersion.Id,
            ContractTypeId = contractTemplateVersion.ContractTypeId,
            Version = contractTemplateVersion.Version,
            TemplateFileUrl = contractTemplateVersion.TemplateFileUrl,
            ContentJson = contractTemplateVersion.ContentJson,
            WorkflowDefinitionId = contractTemplateVersion.WorkflowDefinitionId,
            IsActive = contractTemplateVersion.IsActive,
            CreatedBy = contractTemplateVersion.CreatedBy,
            CreatedAt = contractTemplateVersion.CreatedAt
        };
    }

    public async Task<ContractTemplateVersionDto> CreateAsync(ContractTemplateVersionDto dto)
    {
        var contractTemplateVersion = new ContractTemplateVersion
        {
            ContractTypeId = dto.ContractTypeId,
            Version = dto.Version,
            TemplateFileUrl = dto.TemplateFileUrl,
            ContentJson = dto.ContentJson,
            WorkflowDefinitionId = dto.WorkflowDefinitionId,
            IsActive = dto.IsActive,
            CreatedBy = dto.CreatedBy,
            CreatedAt = dto.CreatedAt
        };

        _context.ContractTemplateVersions.Add(contractTemplateVersion);
        await _context.SaveChangesAsync();

        dto.Id = contractTemplateVersion.Id;
        return dto;
    }

    public async Task<ContractTemplateVersionDto?> UpdateAsync(Guid id, ContractTemplateVersionDto dto)
    {
        var contractTemplateVersion = await _context.ContractTemplateVersions.FindAsync(id);
        if (contractTemplateVersion == null)
            return null;

        contractTemplateVersion.ContractTypeId = dto.ContractTypeId;
        contractTemplateVersion.Version = dto.Version;
        contractTemplateVersion.TemplateFileUrl = dto.TemplateFileUrl;
        contractTemplateVersion.ContentJson = dto.ContentJson;
        contractTemplateVersion.WorkflowDefinitionId = dto.WorkflowDefinitionId;
        contractTemplateVersion.IsActive = dto.IsActive;
        contractTemplateVersion.CreatedBy = dto.CreatedBy;
        contractTemplateVersion.CreatedAt = dto.CreatedAt;

        await _context.SaveChangesAsync();

        return dto;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var contractTemplateVersion = await _context.ContractTemplateVersions.FindAsync(id);
        if (contractTemplateVersion == null)
            return false;

        _context.ContractTemplateVersions.Remove(contractTemplateVersion);
        await _context.SaveChangesAsync();

        return true;
    }
}
