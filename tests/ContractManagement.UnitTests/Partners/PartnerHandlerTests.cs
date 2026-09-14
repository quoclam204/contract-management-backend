using ContractManagement.Application.Common.Behaviors;
using ContractManagement.Application.Features.Partners;
using ContractManagement.Application.Features.Partners.Validators;
using ContractManagement.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContractManagement.UnitTests.Partners;

public class PartnerHandlerTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    [Fact]
    public async Task CreatePartnerCommand_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new CreatePartnerCommandHandler(context);

        var command = new CreatePartnerCommand
        {
            Name = "Công ty Cổ phần Công nghệ Mới",
            TaxCode = "0109876543",
            Representative = "Trần Thị B",
            ContactEmail = "partner@newtech.com",
            Address = "123 Đường Cầu Giấy, Hà Nội"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(command.Name);
        result.TaxCode.Should().Be(command.TaxCode);
        result.Representative.Should().Be(command.Representative);
        result.ContactEmail.Should().Be(command.ContactEmail);
        result.Address.Should().Be(command.Address);

        var partnerInDb = await context.Partners.FirstOrDefaultAsync(p => p.Id == result.Id);
        partnerInDb.Should().NotBeNull();
        partnerInDb!.Name.Should().Be(command.Name);
    }

    [Fact]
    public async Task GetPartnerById_WhenNotFound_ShouldReturnNullOrThrowNotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new GetPartnerByIdQueryHandler(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await handler.Handle(new GetPartnerByIdQuery(nonExistentId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidationBehavior_WhenInvalidCommand_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new CreatePartnerValidator();
        var validators = new List<IValidator<CreatePartnerCommand>> { validator };
        var behavior = new ValidationBehavior<CreatePartnerCommand, PartnerDto>(validators);

        var invalidCommand = new CreatePartnerCommand
        {
            Name = "", // Empty name
            TaxCode = "123", // Too short
            ContactEmail = "invalid-email"
        };

        // Act
        var act = async () => await behavior.Handle(
            invalidCommand,
            () => Task.FromResult(new PartnerDto()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() >= 3);
    }

    [Fact]
    public async Task GetPartnersQueryHandler_WhenSearchTermMatchesName_ShouldReturnMatchingPartners()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        context.Partners.AddRange(
            new Domain.Partner(Guid.NewGuid(), "Công ty Cổ phần Phần mềm FPT", "0101234567", "Nguyễn Văn A", "contact@fpt.com", "Hà Nội", now.AddMinutes(-10)),
            new Domain.Partner(Guid.NewGuid(), "Tập đoàn Viettel", "0109876543", "Trần Văn B", "contact@viettel.vn", "Hà Nội", now.AddMinutes(-5)),
            new Domain.Partner(Guid.NewGuid(), "Công ty TNHH Dịch vụ FPT", "0101234567-001", "Lê Văn C", "service@fpt.com", "TP HCM", now)
        );
        await context.SaveChangesAsync();

        var handler = new GetPartnersQueryHandler(context);
        var query = new GetPartnersQuery { SearchTerm = "fpt" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Name.Contains("FPT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPartnersQueryHandler_WhenSearchTermMatchesTaxCode_ShouldReturnMatchingPartners()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        context.Partners.AddRange(
            new Domain.Partner(Guid.NewGuid(), "Công ty ABC", "0101234567", "Nguyễn Văn A", "abc@gmail.com", "Hà Nội", now.AddMinutes(-10)),
            new Domain.Partner(Guid.NewGuid(), "Công ty XYZ", "0109876543", "Trần Văn B", "xyz@gmail.com", "Hà Nội", now.AddMinutes(-5)),
            new Domain.Partner(Guid.NewGuid(), "Chi nhánh XYZ", "0109876543-001", "Lê Văn C", "xyz-branch@gmail.com", "Đà Nẵng", now)
        );
        await context.SaveChangesAsync();

        var handler = new GetPartnersQueryHandler(context);
        var query = new GetPartnersQuery { SearchTerm = "0109876543" };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.TaxCode != null && p.TaxCode.Contains("0109876543"));
    }

    [Fact]
    public async Task GetPartnersQueryHandler_WithPagination_ShouldReturnCorrectPageAndTotalCount()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        for (int i = 1; i <= 5; i++)
        {
            context.Partners.Add(new Domain.Partner(
                Guid.NewGuid(),
                $"Đối tác thứ {i}",
                $"010000000{i}",
                $"Đại diện {i}",
                $"partner{i}@test.com",
                $"Địa chỉ {i}",
                now.AddMinutes(-i)
            ));
        }
        await context.SaveChangesAsync();

        var handler = new GetPartnersQueryHandler(context);
        var query = new GetPartnersQuery { PageNumber = 2, PageSize = 2 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }
}

