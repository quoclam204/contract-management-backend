using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ContractManagement.Domain.Identity.Entities;
using ContractManagement.Domain.Identity.Enums;
using ContractManagement.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ContractManagement.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "SuperSecretTestingKeyForJwtTokenService2026!@#$"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:ExpiresInMinutes", "60"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_ValidUser_ShouldGenerateValidJwtWithExpectedClaims()
    {
        // Arrange
        var user = new User("Nguyen Van A", "vana@test.com", "hash", UserRole.Manager, Guid.NewGuid());

        // Act
        var token = _tokenService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");

        var claims = jwtToken.Claims.ToList();
        claims.First(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(user.Id.ToString());
        claims.First(c => c.Type == "email" || c.Type == ClaimTypes.Email).Value.Should().Be(user.Email);
        claims.First(c => c.Type == "unique_name" || c.Type == ClaimTypes.Name).Value.Should().Be(user.FullName);
        claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value.Should().Be("Manager");
        claims.First(c => c.Type == "role_id").Value.Should().Be("1");
        claims.First(c => c.Type == "department_id").Value.Should().Be(user.DepartmentId!.Value.ToString());
    }

    [Fact]
    public void GenerateToken_UserWithoutDepartment_ShouldNotContainDepartmentIdClaim()
    {
        // Arrange
        var user = new User("Admin User", "admin@test.com", "hash", UserRole.Admin, null);

        // Act
        var token = _tokenService.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().NotContain(c => c.Type == "department_id");
    }
}
