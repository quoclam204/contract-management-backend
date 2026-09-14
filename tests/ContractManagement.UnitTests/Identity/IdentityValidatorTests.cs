using ContractManagement.Application.Identity.DTOs;
using ContractManagement.Application.Identity.Validators;
using ContractManagement.Domain.Identity.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace ContractManagement.UnitTests.Identity;

public class IdentityValidatorTests
{
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly RegisterUserValidator _registerValidator = new();
    private readonly CreateUserValidator _createUserValidator = new();
    private readonly CreateDepartmentValidator _createDeptValidator = new();
    private readonly UpdateDepartmentValidator _updateDeptValidator = new();

    [Fact]
    public void LoginRequestValidator_InvalidEmail_ShouldHaveValidationError()
    {
        var model = new LoginRequestDto("not-an-email", "Password123");
        var result = _loginValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void LoginRequestValidator_EmptyPassword_ShouldHaveValidationError()
    {
        var model = new LoginRequestDto("user@test.com", "");
        var result = _loginValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterUserValidator_ShortPassword_ShouldHaveValidationError()
    {
        var model = new RegisterUserDto("Nguyen Van A", "a@test.com", "12345");
        var result = _registerValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void CreateUserValidator_ValidDto_ShouldNotHaveValidationError()
    {
        var model = new CreateUserDto("Admin User", "admin@test.com", "Password123!", UserRole.Admin);
        var result = _createUserValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateDepartmentValidator_EmptyName_ShouldHaveValidationError()
    {
        var model = new CreateDepartmentDto("");
        var result = _createDeptValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateDepartmentValidator_EmptyName_ShouldHaveValidationError()
    {
        var model = new UpdateDepartmentDto("");
        var result = _updateDeptValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
