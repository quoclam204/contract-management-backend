using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Application.Identity.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IIdentityDbContext _dbContext;

    public DepartmentService(IIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.ManagerId, d.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
            return null;

        return new DepartmentDto(department.Id, department.Name, department.ManagerId, department.CreatedAt);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Department name cannot be empty", nameof(dto.Name));

        var department = new Department(dto.Name.Trim(), dto.ManagerId);

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(department.Id, department.Name, department.ManagerId, department.CreatedAt);
    }

    public async Task<DepartmentDto?> UpdateAsync(Guid id, UpdateDepartmentDto dto, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Department name cannot be empty", nameof(dto.Name));

        department.Name = dto.Name.Trim();
        department.ManagerId = dto.ManagerId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(department.Id, department.Name, department.ManagerId, department.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
            return false;

        _dbContext.Departments.Remove(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
