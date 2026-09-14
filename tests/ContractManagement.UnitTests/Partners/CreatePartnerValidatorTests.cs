using ContractManagement.Application.Features.Partners;
using ContractManagement.Application.Features.Partners.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace ContractManagement.UnitTests.Partners;

public class CreatePartnerValidatorTests
{
    private readonly CreatePartnerValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("12345")] // Sai độ dài (< 10)
    [InlineData("123456789")] // 9 ký tự (< 10)
    [InlineData("123456789012345")] // 15 ký tự (> 14)
    [InlineData("012345678A")] // Chứa chữ cái
    [InlineData("0123456789001")] // 13 chữ số không có dấu gạch nối
    [InlineData("0123456789-01")] // Chi nhánh thiếu số
    [InlineData("0123456789-0001")] // Chi nhánh thừa số
    [InlineData("0123456789-ABC")] // Chi nhánh chứa chữ cái
    public void CreatePartnerValidator_InvalidTaxCode_ShouldHaveValidationError(string? taxCode)
    {
        // Arrange
        var command = new CreatePartnerCommand
        {
            Name = "Công ty TNHH ABC",
            TaxCode = taxCode!,
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
    [InlineData("0123456789")] // 10 chữ số chuẩn doanh nghiệp
    [InlineData("0123456789-001")] // 13 chữ số chuẩn chi nhánh có dấu gạch nối
    public void CreatePartnerValidator_ValidTaxCode_ShouldNotHaveValidationErrorForTaxCode(string taxCode)
    {
        // Arrange
        var command = new CreatePartnerCommand
        {
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

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("plainaddress")]
    [InlineData("@missingusername.com")]
    [InlineData("missingdomain@")]
    [InlineData("")]
    public void CreatePartnerValidator_InvalidEmail_ShouldHaveValidationError(string email)
    {
        // Arrange
        var command = new CreatePartnerCommand
        {
            Name = "Công ty TNHH ABC",
            TaxCode = "0101234567",
            ContactEmail = email,
            Representative = "Nguyễn Văn A",
            Address = "Hà Nội"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Theory]
    [InlineData("0101234567")] // 10 chữ số
    [InlineData("0101234567-001")] // 13 chữ số có dấu gạch nối
    public void CreatePartnerValidator_ValidData_ShouldNotHaveAnyValidationErrors(string taxCode)
    {
        // Arrange
        var command = new CreatePartnerCommand
        {
            Name = "Công ty TNHH ABC",
            TaxCode = taxCode,
            ContactEmail = "contact@abc.vn",
            Representative = "Nguyễn Văn A",
            Address = "Hà Nội"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
