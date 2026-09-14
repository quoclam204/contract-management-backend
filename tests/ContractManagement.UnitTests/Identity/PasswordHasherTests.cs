using ContractManagement.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace ContractManagement.UnitTests.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ValidPassword_ShouldReturnSaltAndHashSeparatedByDot()
    {
        // Act
        var hash = _hasher.HashPassword("TestPassword123!");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        var parts = hash.Split('.');
        parts.Should().HaveCount(2);
        parts[0].Should().NotBeEmpty();
        parts[1].Should().NotBeEmpty();
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "MySecurePassword2026";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "MySecurePassword2026";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword("WrongPassword", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidHashFormat_ShouldReturnFalse()
    {
        // Act
        var result = _hasher.VerifyPassword("password", "invalid-hash-without-dot");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ShouldProduceDifferentHashesDueToRandomSalt()
    {
        // Arrange
        const string password = "SamePassword123";

        // Act
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
        _hasher.VerifyPassword(password, hash1).Should().BeTrue();
        _hasher.VerifyPassword(password, hash2).Should().BeTrue();
    }
}
