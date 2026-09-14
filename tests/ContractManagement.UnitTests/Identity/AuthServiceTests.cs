using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Application.Identity.Services;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Identity.Enums;
using ContractManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ContractManagement.UnitTests.Identity;

public class AuthServiceTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly Mock<IJwtTokenService> _mockJwtTokenService = new();

    public AuthServiceTests()
    {
        _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns<string>(p => $"hashed_{p}");

        _mockPasswordHasher.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((pwd, hash) => hash == $"hashed_{pwd}");

        _mockJwtTokenService.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns("mock-jwt-token");
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ShouldCreateUserWithStaffRole()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        // Even if client attempts to pass Admin role in RegisterUserDto, it should be saved as Staff
        var request = new RegisterUserDto("Nhan Vien Moi", "staff@test.com", "Password123!", null, UserRole.Admin);

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("staff@test.com");
        result.FullName.Should().Be("Nhan Vien Moi");
        result.Role.Should().Be(UserRole.Staff); // Verified: Staff role enforced

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Staff);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        var existingUser = new User("User 1", "duplicate@test.com", "hash", UserRole.Staff);
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var request = new RegisterUserDto("User 2", "duplicate@test.com", "Password123!");

        // Act
        var act = async () => await authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_InvalidDepartment_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        var request = new RegisterUserDto("User", "user@test.com", "Password123!", Guid.NewGuid());

        // Act
        var act = async () => await authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*department does not exist*");
    }

    [Fact]
    public async Task CreateUserAsync_ByAdmin_ShouldAssignRequestedRole()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        var request = new CreateUserDto("Admin Manager", "manager@test.com", "Password123!", UserRole.Manager);

        // Act
        var result = await authService.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be(UserRole.Manager);

        var saved = await context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        saved!.Role.Should().Be(UserRole.Manager);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnTokenAndUserDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        var user = new User("Dang Nhap", "login@test.com", "hashed_Secret123!", UserRole.Staff);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto("login@test.com", "Secret123!");

        // Act
        var response = await authService.LoginAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Token.Should().Be("mock-jwt-token");
        response.User.Email.Should().Be("login@test.com");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        var user = new User("User", "user@test.com", "hashed_CorrectPassword", UserRole.Staff);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto("user@test.com", "WrongPassword");

        // Act
        var act = async () => await authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_InactiveAccount_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _mockPasswordHasher.Object, _mockJwtTokenService.Object);

        var user = new User("Inactive User", "inactive@test.com", "hashed_Secret123!", UserRole.Staff)
        {
            IsActive = false
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto("inactive@test.com", "Secret123!");

        // Act
        var act = async () => await authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*inactive*");
    }
}
