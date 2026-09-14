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

public class ContractTypeServiceTests : IDisposable
{
    private readonly ContractManagementDbContext _context;
    private readonly ContractTypeService _service;

    public ContractTypeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _context = new ContractManagementDbContext(options);
        _service = new ContractTypeService(_context);
        
        // Seed test data
        _context.ContractTypes.AddRange(
            new ContractType { Id = Guid.NewGuid(), Name = "Test Type 1", CreatedAt = DateTime.UtcNow },
            new ContractType { Id = Guid.NewGuid(), Name = "Test Type 2", CreatedAt = DateTime.UtcNow }
        );
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllContractTypes()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, ct => ct.Name == "Test Type 1");
        Assert.Contains(result, ct => ct.Name == "Test Type 2");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsContractType()
    {
        // Arrange
        var testId = _context.ContractTypes.First().Id;

        // Act
        var result = await _service.GetByIdAsync(testId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testId, result.Id);
        Assert.Equal("Test Type 1", result.Name);
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
    public async Task CreateAsync_AddsAndReturnsContractType()
    {
        // Arrange
        var dto = new ContractTypeDto { Name = "New Type", CreatedAt = DateTime.UtcNow };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.CreatedAt, result.CreatedAt);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingId_UpdatesAndReturnsContractType()
    {
        // Arrange
        var existingType = _context.ContractTypes.First();
        var dto = new ContractTypeDto { Id = existingType.Id, Name = "Updated Name", CreatedAt = DateTime.UtcNow };

        // Act
        var result = await _service.UpdateAsync(existingType.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.CreatedAt, result.CreatedAt);
        
        // Verify the entity was actually updated in the context
        var updatedEntity = await _context.ContractTypes.FindAsync(existingType.Id);
        Assert.Equal("Updated Name", updatedEntity?.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var dto = new ContractTypeDto { Id = Guid.NewGuid(), Name = "Test", CreatedAt = DateTime.UtcNow };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var testId = _context.ContractTypes.First().Id;
        var initialCount = _context.ContractTypes.Count();

        // Act
        var result = await _service.DeleteAsync(testId);

        // Assert
        Assert.True(result);
        Assert.Equal(initialCount - 1, _context.ContractTypes.Count());
        Assert.Null(await _context.ContractTypes.FindAsync(testId));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var initialCount = _context.ContractTypes.Count();

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        Assert.Equal(initialCount, _context.ContractTypes.Count());
    }
}
