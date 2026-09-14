using ContractManagement.Application.Features.Partners;
using ContractManagement.Application.Features.Partners.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace ContractManagement.UnitTests.Partners;

public class UpdatePartnerValidatorTests
{
    private readonly UpdatePartnerValidator _validator = new();

    [Fact]
    public void UpdatePartnerValidator_WhenIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new UpdatePartnerCommand
        {
            Id = Guid.Empty,
            Name = "Công ty TNHH ABC",
            TaxCode = "0101234567",
            ContactEmail = "contact@abc.vn",
            Representative = "Nguyễn Văn A",
            Address = "Hà Nội"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12345")]
    [InlineData("012345678A")]
    [InlineData("0123456789001")]
    public void UpdatePartnerValidator_InvalidTaxCode_ShouldHaveValidationError(string taxCode)
    {
        // Arrange
        var command = new UpdatePartnerCommand
        {
            Id = Guid.NewGuid(),
            Name = "Công ty TNHH ABC",
            TaxCode = taxCode,
            ContactEmail = "contact@abc.vn",
            Representative = "Nguyễn Văn A",
            Address = "Hà Nội"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaxCode);
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("0123456789-001")]
    public void UpdatePartnerValidator_ValidTaxCode_ShouldNotHaveValidationErrorForTaxCode(string taxCode)
    {
        // Arrange
        var command = new UpdatePartnerCommand
        {
            Id = Guid.NewGuid(),
            Name = "Công ty TNHH ABC",
            TaxCode = taxCode,
            ContactEmail = "contact@abc.vn",
            Representative = "Nguyễn Văn A",
            Address = "Hà Nội"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TaxCode);
    }
}
