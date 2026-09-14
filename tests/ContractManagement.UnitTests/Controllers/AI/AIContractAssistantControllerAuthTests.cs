using ContractManagement.Api.Controllers.AI;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ContractManagement.UnitTests.Controllers.AI;

/// <summary>
/// Tests to verify AIContractAssistantController authentication requirements.
///
/// Note: These tests verify the [Authorize] attribute is present on the controller.
/// Full integration tests of JWT authentication behavior would require HTTP client setup
/// with test JWT tokens, which is beyond the scope of this unit test suite.
/// </summary>
public class AIContractAssistantControllerAuthTests
{
    [Fact]
    public void AIContractAssistantController_HasAuthorizeAttribute()
    {
        // Arrange
        var controllerType = typeof(AIContractAssistantController);

        // Act
        var authorizeAttributes = controllerType.GetCustomAttributes(
            typeof(AuthorizeAttribute),
            inherit: false);

        // Assert
        Assert.NotEmpty(authorizeAttributes);
        Assert.Single(authorizeAttributes);
        Assert.IsType<AuthorizeAttribute>(authorizeAttributes[0]);
    }

    [Fact]
    public void AIContractAssistantController_AuthorizeAttributeHasNoPolicy()
    {
        // Arrange
        var controllerType = typeof(AIContractAssistantController);
        var authorizeAttributes = controllerType.GetCustomAttributes(
            typeof(AuthorizeAttribute),
            inherit: false);
        var authorizeAttribute = authorizeAttributes[0] as AuthorizeAttribute;

        // Act & Assert
        // [Authorize] without policy means any authenticated user can access
        Assert.NotNull(authorizeAttribute);
        Assert.Null(authorizeAttribute.Policy);
        Assert.Null(authorizeAttribute.Roles);
    }

    [Fact]
    public void AIContractAssistantController_RequiresAuthentication()
    {
        // Arrange
        var controllerType = typeof(AIContractAssistantController);

        // Act
        var attributes = controllerType.GetCustomAttributes(inherit: false);
        var hasAuthorize = attributes.OfType<AuthorizeAttribute>().Any();

        // Assert
        Assert.True(hasAuthorize, "AIContractAssistantController should have [Authorize] attribute to require authentication");
    }
}
