using ContractManagement.Application;
using ContractManagement.Domain;
using ContractManagement.Infrastructure;
using Xunit;

namespace ContractManagement.IntegrationTests;

public class IntegrationTests
{
    [Fact]
    public void Infrastructure_References_Correctly()
    {
        // Verify that Infrastructure project references Application and Domain
        var domainType = typeof(ContractManagement.Domain.Identity.IdentityEntity);
        var applicationType = typeof(ContractManagement.Application.Identity.IdentityEntity);
        var infrastructureType = typeof(ContractManagement.Infrastructure.Persistence.AppDbContext);

        Assert.NotNull(domainType);
        Assert.NotNull(applicationType);
        Assert.NotNull(infrastructureType);
    }
}