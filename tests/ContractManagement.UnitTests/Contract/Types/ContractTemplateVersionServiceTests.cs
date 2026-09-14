using ContractManagement.Application.Contract.DTOs;
using ContractManagement.Application.Contract.Services;
using ContractManagement.Domain.Contract.Entities;
using ContractManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ContractManagement.UnitTests.Contract.Types;

public class ContractTemplateVersionServiceTests : IDisposable
{
    private readonly ContractManagementDbContext _context;
    private readonly ContractTemplateVersionService _service;

    public ContractTemplateVersionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _context = new ContractManagementDbContext(options);
        _service = new ContractTemplateVersionService(_context);
        
        // Seed test data
        var contractTypeId = Guid.NewGuid();
        _context.ContractTemplateVersions.AddRange(
            new ContractTemplateVersion { Id = Guid.NewGuid(), ContractTypeId = contractTypeId, Version = 1, IsActive = true, CreatedBy = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
            new ContractTemplateVersion { Id = Guid.NewGuid(), ContractTypeId = contractTypeId, Version = 2, IsActive = false, CreatedBy = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
        );
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllContractTemplateVersions()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, ctv => ctv.Version == 1 && ctv.IsActive == true);
        Assert.Contains(result, ctv => ctv.Version == 2 && ctv.IsActive == false);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsContractTemplateVersion()
    {
        // Arrange
        var testId = _context.ContractTemplateVersions.First().Id;

        // Act
        var result = await _service.GetByIdAsync(testId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testId, result.Id);
        Assert.Equal(1, result.Version);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_AddsAndReturnsContractTemplateVersion()
    {
        // Arrange
        var dto = new ContractTemplateVersionDto 
        { 
            ContractTypeId = Guid.NewGuid(), 
            Version = 1, 
            IsActive = true, 
            CreatedBy = Guid.NewGuid(), 
            CreatedAt = DateTime.UtcNow 
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.ContractTypeId, result.ContractTypeId);
        Assert.Equal(dto.Version, result.Version);
        Assert.Equal(dto.IsActive, result.IsActive);
        Assert.Equal(dto.CreatedBy, result.CreatedBy);
        Assert.Equal(dto.CreatedAt, result.CreatedAt);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingId_UpdatesAndReturnsContractTemplateVersion()
    {
        // Arrange
        var existingTemplateVersion = _context.ContractTemplateVersions.First();
        var dto = new ContractTemplateVersionDto { Id = existingTemplateVersion.Id, ContractTypeId = existingTemplateVersion.ContractTypeId, Version = 2, IsActive = true, CreatedBy = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

        // Act
        var result = await _service.UpdateAsync(existingTemplateVersion.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.ContractTypeId, result.ContractTypeId);
        Assert.Equal(dto.Version, result.Version);
        Assert.Equal(dto.IsActive, result.IsActive);
        Assert.Equal(dto.CreatedBy, result.CreatedBy);
        Assert.Equal(dto.CreatedAt, result.CreatedAt);
        
        // Verify the entity was actually updated in the context
        var updatedEntity = await _context.ContractTemplateVersions.FindAsync(existingTemplateVersion.Id);
        Assert.Equal(2, updatedEntity?.Version);
        Assert.True(updatedEntity?.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var dto = new ContractTemplateVersionDto { Id = Guid.NewGuid(), ContractTypeId = Guid.NewGuid(), Version = 1, IsActive = true, CreatedBy = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var testId = _context.ContractTemplateVersions.First().Id;
        var initialCount = _context.ContractTemplateVersions.Count();

        // Act
        var result = await _service.DeleteAsync(testId);

        // Assert
        Assert.True(result);
        Assert.Equal(initialCount - 1, _context.ContractTemplateVersions.Count());
        Assert.Null(await _context.ContractTemplateVersions.FindAsync(testId));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var initialCount = _context.ContractTemplateVersions.Count();

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        Assert.Equal(initialCount, _context.ContractTemplateVersions.Count());
    }
}
