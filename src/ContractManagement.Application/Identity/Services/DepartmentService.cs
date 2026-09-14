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

        var nameTrimmed = dto.Name.Trim();
        var exists = await _dbContext.Departments
            .AnyAsync(d => d.Name == nameTrimmed, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Department with name '{nameTrimmed}' already exists.");

        if (dto.ManagerId.HasValue)
        {
            var managerExists = await _dbContext.Users
                .AnyAsync(u => u.Id == dto.ManagerId.Value, cancellationToken);
            if (!managerExists)
                throw new ArgumentException("Specified manager user does not exist.", nameof(dto.ManagerId));
        }

        var department = new Department(nameTrimmed, dto.ManagerId);

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

        var nameTrimmed = dto.Name.Trim();
        var duplicate = await _dbContext.Departments
            .AnyAsync(d => d.Id != id && d.Name == nameTrimmed, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException($"Another department with name '{nameTrimmed}' already exists.");

        if (dto.ManagerId.HasValue)
        {
            var managerExists = await _dbContext.Users
                .AnyAsync(u => u.Id == dto.ManagerId.Value, cancellationToken);
            if (!managerExists)
                throw new ArgumentException("Specified manager user does not exist.", nameof(dto.ManagerId));
        }

        department.Name = nameTrimmed;
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

        var hasUsers = await _dbContext.Users
            .AnyAsync(u => u.DepartmentId == id, cancellationToken);
        if (hasUsers)
            throw new InvalidOperationException("Cannot delete department because users are assigned to it.");

        _dbContext.Departments.Remove(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
