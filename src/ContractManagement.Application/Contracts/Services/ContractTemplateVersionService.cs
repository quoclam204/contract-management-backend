using ContractManagement.Application.Contracts.DTOs;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Domain.Contracts.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contracts.Services;

/// <summary>
/// Service for managing contract template versions.
/// </summary>
public class ContractTemplateVersionService : IContractTemplateVersionService
{
    private readonly IContractDbContext _context;

    public ContractTemplateVersionService(IContractDbContext context)
    {
        _context = context;
    }

    public async Task<ContractTemplateVersionDto> CreateContractTemplateVersionAsync(CreateContractTemplateVersionRequest request)
    {
        var templateVersion = new ContractTemplateVersion
        {
            ContractTypeId = request.ContractTypeId,
            Version = request.Version,
            TemplateFileUrl = request.TemplateFileUrl,
            ContentJson = request.ContentJson,
            WorkflowDefinitionId = request.WorkflowDefinitionId,
            IsActive = request.IsActive,
            CreatedBy = request.CreatedBy
        };

        _context.ContractTemplateVersions.Add(templateVersion);
        await _context.SaveChangesAsync();

        return new ContractTemplateVersionDto
        {
            Id = templateVersion.Id,
            ContractTypeId = templateVersion.ContractTypeId,
            Version = templateVersion.Version,
            TemplateFileUrl = templateVersion.TemplateFileUrl,
            ContentJson = templateVersion.ContentJson,
            WorkflowDefinitionId = templateVersion.WorkflowDefinitionId,
            IsActive = templateVersion.IsActive,
            CreatedBy = templateVersion.CreatedBy,
            CreatedAt = templateVersion.CreatedAt
        };
    }

    public async Task<ContractTemplateVersionDto?> GetContractTemplateVersionByIdAsync(Guid id)
    {
        var templateVersion = await _context.ContractTemplateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(tv => tv.Id == id);

        if (templateVersion == null)
            return null;

        return new ContractTemplateVersionDto
        {
            Id = templateVersion.Id,
            ContractTypeId = templateVersion.ContractTypeId,
            Version = templateVersion.Version,
            TemplateFileUrl = templateVersion.TemplateFileUrl,
            ContentJson = templateVersion.ContentJson,
            WorkflowDefinitionId = templateVersion.WorkflowDefinitionId,
            IsActive = templateVersion.IsActive,
            CreatedBy = templateVersion.CreatedBy,
            CreatedAt = templateVersion.CreatedAt
        };
    }

    public async Task<ContractTemplateVersionDto> UpdateContractTemplateVersionAsync(Guid id, UpdateContractTemplateVersionRequest request)
    {
        var templateVersion = await _context.ContractTemplateVersions.FindAsync(id);
        if (templateVersion == null)
            throw new KeyNotFoundException($"Contract template version with Id {id} not found.");

        if (!string.IsNullOrWhiteSpace(request.TemplateFileUrl))
            templateVersion.TemplateFileUrl = request.TemplateFileUrl;
        if (!string.IsNullOrWhiteSpace(request.ContentJson))
            templateVersion.ContentJson = request.ContentJson;
        if (request.WorkflowDefinitionId.HasValue)
            templateVersion.WorkflowDefinitionId = request.WorkflowDefinitionId.Value;
        if (request.IsActive.HasValue)
            templateVersion.IsActive = request.IsActive.Value;

        _context.ContractTemplateVersions.Update(templateVersion);
        await _context.SaveChangesAsync();

        return new ContractTemplateVersionDto
        {
            Id = templateVersion.Id,
            ContractTypeId = templateVersion.ContractTypeId,
            Version = templateVersion.Version,
            TemplateFileUrl = templateVersion.TemplateFileUrl,
            ContentJson = templateVersion.ContentJson,
            WorkflowDefinitionId = templateVersion.WorkflowDefinitionId,
            IsActive = templateVersion.IsActive,
            CreatedBy = templateVersion.CreatedBy,
            CreatedAt = templateVersion.CreatedAt
        };
    }

    public async Task<bool> DeleteContractTemplateVersionAsync(Guid id)
    {
        var templateVersion = await _context.ContractTemplateVersions.FindAsync(id);
        if (templateVersion == null)
            return false;

        _context.ContractTemplateVersions.Remove(templateVersion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ContractTemplateVersionDto>> GetContractTemplateVersionsAsync()
    {
        return await _context.ContractTemplateVersions
            .AsNoTracking()
            .Select(tv => new ContractTemplateVersionDto
            {
                Id = tv.Id,
                ContractTypeId = tv.ContractTypeId,
                Version = tv.Version,
                TemplateFileUrl = tv.TemplateFileUrl,
                ContentJson = tv.ContentJson,
                WorkflowDefinitionId = tv.WorkflowDefinitionId,
                IsActive = tv.IsActive,
                CreatedBy = tv.CreatedBy,
                CreatedAt = tv.CreatedAt
            })
            .ToListAsync();
    }
}