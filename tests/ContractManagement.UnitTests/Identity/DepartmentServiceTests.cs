using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Services;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Identity.Enums;
using ContractManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContractManagement.UnitTests.Identity;

public class DepartmentServiceTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var dto = new CreateDepartmentDto("Phòng Kỹ Thuật");

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Phòng Kỹ Thuật");
        result.ManagerId.Should().BeNull();

        var saved = await context.Departments.FirstOrDefaultAsync(d => d.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Phòng Kỹ Thuật");
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var dto = new CreateDepartmentDto("   ");

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Department name cannot be empty*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        context.Departments.Add(new Department("Phòng Kế Toán"));
        await context.SaveChangesAsync();

        var dto = new CreateDepartmentDto("Phòng Kế Toán");

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_NonExistentManagerId_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var nonExistentManagerId = Guid.NewGuid();
        var dto = new CreateDepartmentDto("Phòng Nhân Sự", nonExistentManagerId);

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*manager user does not exist*");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDepartments()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        context.Departments.AddRange(
            new Department("Phòng A"),
            new Department("Phòng B")
        );
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetAllAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnDepartment()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var dept = new Department("Phòng Dự Án");
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(dept.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(dept.Id);
        result.Name.Should().Be("Phòng Dự Án");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var dept = new Department("Tên Cũ");
        context.Departments.Add(dept);

        var manager = new User("Manager A", "manager@test.com", "hash", UserRole.Manager);
        context.Users.Add(manager);
        await context.SaveChangesAsync();

        var updateDto = new UpdateDepartmentDto("Tên Mới", manager.Id);

        // Act
        var result = await service.UpdateAsync(dept.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Tên Mới");
        result.ManagerId.Should().Be(manager.Id);
    }

    [Fact]
    public async Task DeleteAsync_DepartmentWithNoUsers_ShouldDeleteSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var dept = new Department("Phòng Tạm");
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        // Act
        var success = await service.DeleteAsync(dept.Id);

        // Assert
        success.Should().BeTrue();
        (await context.Departments.AnyAsync(d => d.Id == dept.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DepartmentWithUsers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var dept = new Department("Phòng Đang Có Người");
        context.Departments.Add(dept);

        var user = new User("Nhan Vien A", "staff@test.com", "hash", UserRole.Staff, dept.Id);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var act = async () => await service.DeleteAsync(dept.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*users are assigned to it*");
    }
}
