using ContractManagement.Application.Contract.DTOs;
using ContractManagement.Application.Contract.Interfaces;
using ContractManagement.Domain.Contract.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Contract.Services;

public class ContractTypeService : IContractTypeService
{
    private readonly IContractManagementDbContext _context;

    public ContractTypeService(IContractManagementDbContext context)
    {
        _context = context;
    }

    public async Task<List<ContractTypeDto>> GetAllAsync()
    {
        return await _context.ContractTypes
            .Select(ct => new ContractTypeDto
            {
                Id = ct.Id,
                Name = ct.Name,
                CreatedAt = ct.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ContractTypeDto?> GetByIdAsync(Guid id)
    {
        var contractType = await _context.ContractTypes.FindAsync(id);
        if (contractType == null)
            return null;

        return new ContractTypeDto
        {
            Id = contractType.Id,
            Name = contractType.Name,
            CreatedAt = contractType.CreatedAt
        };
    }

    public async Task<ContractTypeDto> CreateAsync(ContractTypeDto dto)
    {
        var contractType = new ContractType
        {
            Name = dto.Name,
            CreatedAt = dto.CreatedAt
        };

        _context.ContractTypes.Add(contractType);
        await _context.SaveChangesAsync();

        dto.Id = contractType.Id;
        return dto;
    }

    public async Task<ContractTypeDto?> UpdateAsync(Guid id, ContractTypeDto dto)
    {
        var contractType = await _context.ContractTypes.FindAsync(id);
        if (contractType == null)
            return null;

        contractType.Name = dto.Name;
        contractType.CreatedAt = dto.CreatedAt;

        await _context.SaveChangesAsync();

        return dto;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var contractType = await _context.ContractTypes.FindAsync(id);
        if (contractType == null)
            return false;

        _context.ContractTypes.Remove(contractType);
        await _context.SaveChangesAsync();

        return true;
    }
}
