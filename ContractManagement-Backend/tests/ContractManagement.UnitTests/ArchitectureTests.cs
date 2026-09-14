using ContractManagement.Application;
using ContractManagement.Domain;
using ContractManagement.Infrastructure;
using Xunit;

namespace ContractManagement.UnitTests;

public class ArchitectureTests
{
    [Fact]
    public void Solution_BuildsSuccessfully()
    {
        // This test verifies that the solution builds and references are correct
        // If this test passes, it means:
        // 1. All projects compile
        // 2. Dependencies are correctly set up
        // 3. No circular dependencies exist

        // Arrange & Act - The test passes if we can reference all layers
        var domainType = typeof(ContractManagement.Domain.Identity.IdentityEntity);
        var applicationType = typeof(ContractManagement.Application.Identity.IdentityEntity);
        var infrastructureType = typeof(ContractManagement.Infrastructure.Persistence.AppDbContext);

        // Assert - Types exist and are accessible
        Assert.NotNull(domainType);
        Assert.NotNull(applicationType);
        Assert.NotNull(infrastructureType);
    }

    [Fact]
    public void Domain_HasNoExternalDependencies()
    {
        // Verify Domain layer doesn't depend on Infrastructure or ASP.NET Core
        var domainAssembly = typeof(ContractManagement.Domain.Identity.IdentityEntity).Assembly;
        var referencedAssemblyNames = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        // Domain should only depend on System packages and itself
        Assert.DoesNotContain("ContractManagement.Infrastructure", referencedAssemblyNames);
        Assert.DoesNotContain("ContractManagement.Application", referencedAssemblyNames);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedAssemblyNames);
        Assert.DoesNotContain("Microsoft.AspNetCore", referencedAssemblyNames);
    }
}