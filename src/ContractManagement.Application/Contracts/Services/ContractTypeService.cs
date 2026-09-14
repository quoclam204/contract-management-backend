using ContractManagement.Application.Contracts.DTOs;
using ContractManagement.Application.Contracts.Interfaces;
using ContractManagement.Domain.Contracts.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contracts.Services;

/// <summary>
/// Service for managing contract types.
/// </>
public class ContractTypeService : IContractTypeService
{
    private readonly IContractDbContext _context;

    public ContractTypeService(IContractDbContext context)
    {
        _context = context;
    }

    public async Task<ContractTypeDto> CreateContractTypeAsync(CreateContractTypeRequest request)
    {
        var contractType = new ContractType
        {
            Name = request.Name
        };

        _context.ContractTypes.Add(contractType);
        await _context.SaveChangesAsync();

        return new ContractTypeDto
        {
            Id = contractType.Id,
            Name = contractType.Name,
            CreatedAt = contractType.CreatedAt
        };
    }

    public async Task<ContractTypeDto?> GetContractTypeByIdAsync(Guid id)
    {
        var contractType = await _context.ContractTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Id == id);

        if (contractType == null)
            return null;

        return new ContractTypeDto
        {
            Id = contractType.Id,
            Name = contractType.Name,
            CreatedAt = contractType.CreatedAt
        };
    }

    public async Task<List<ContractTypeDto>> GetContractTypesAsync()
    {
        return await _context.ContractTypes
            .AsNoTracking()
            .Select(ct => new ContractTypeDto
            {
                Id = ct.Id,
                Name = ct.Name,
                CreatedAt = ct.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ContractTypeDto> UpdateContractTypeAsync(Guid id, UpdateContractTypeRequest request)
    {
        var contractType = await _context.ContractTypes.FindAsync(id);
        if (contractType == null)
            throw new KeyNotFoundException($"Contract type with Id {id} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            contractType.Name = request.Name;

        _context.ContractTypes.Update(contractType);
        await _context.SaveChangesAsync();

        return new ContractTypeDto
        {
            Id = contractType.Id,
            Name = contractType.Name,
            CreatedAt = contractType.CreatedAt
        };
    }

    public async Task<bool> DeleteContractTypeAsync(Guid id)
    {
        var contractType = await _context.ContractTypes.FindAsync(id);
        if (contractType == null)
            return false;

        _context.ContractTypes.Remove(contractType);
        await _context.SaveChangesAsync();
        return true;
    }
}