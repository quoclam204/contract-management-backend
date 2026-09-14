using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Attachments;
using ContractManagement.Application.Features.Attachments.Validators;
using ContractManagement.Domain;
using ContractManagement.Domain.Contract.Entities;
using ContractManagement.Infrastructure.Persistence;
using ContractManagement.Infrastructure.Services;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ContractManagement.UnitTests.Attachments;

public class AttachmentHandlerTests
{
    private static ContractManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ContractManagementDbContext(options);
    }

    private static ContractManagement.Domain.Contract.Entities.Contract SeedContract(ContractManagementDbContext context)
    {
        var contract = new ContractManagement.Domain.Contract.Entities.Contract
        {
            Id = Guid.NewGuid(),
            ContractNumber = "HD-" + Guid.NewGuid().ToString()[..8],
            Title = "Hợp đồng thử nghiệm",
            EffectiveDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            ContractTypeId = Guid.NewGuid(),
            TemplateVersionUsedId = Guid.NewGuid(),
            PartnerId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        context.Contracts.Add(contract);
        context.SaveChanges();
        return contract;
    }

    [Fact]
    public async Task UploadAttachment_WhenFirstFile_ShouldSetVersion1()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var contract = SeedContract(context);

        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.SaveFileAsync(
                contract.Id,
                1,
                "hop_dong_goc.pdf",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"storage/contracts/{contract.Id}/v1_hop_dong_goc.pdf");

        var mockCurrentUser = new Mock<ICurrentUserService>();
        var currentUserId = Guid.NewGuid();
        mockCurrentUser.Setup(u => u.UserId).Returns(currentUserId);

        var handler = new UploadAttachmentCommandHandler(context, mockStorage.Object, mockCurrentUser.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        var command = new UploadAttachmentCommand
        {
            ContractId = contract.Id,
            FileName = "hop_dong_goc.pdf",
            FileStream = stream
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContractId.Should().Be(contract.Id);
        result.FileName.Should().Be("hop_dong_goc.pdf");
        result.Version.Should().Be(1);
        result.FileUrl.Should().Be($"storage/contracts/{contract.Id}/v1_hop_dong_goc.pdf");
        result.UploadedBy.Should().Be(currentUserId);

        var inDb = await context.Attachments.FirstOrDefaultAsync(a => a.Id == result.Id);
        inDb.Should().NotBeNull();
        inDb!.Version.Should().Be(1);
    }

    [Fact]
    public async Task UploadAttachment_WhenExistingAttachments_ShouldIncrementMaxVersion()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var contract = SeedContract(context);
        var uploaderId = Guid.NewGuid();

        // Seed Version 1 and Version 2
        context.Attachments.AddRange(
            new Attachment(Guid.NewGuid(), contract.Id, "file_v1.pdf", 1, "storage/path1", uploaderId, DateTime.UtcNow.AddHours(-2)),
            new Attachment(Guid.NewGuid(), contract.Id, "file_v2.pdf", 2, "storage/path2", uploaderId, DateTime.UtcNow.AddHours(-1))
        );
        await context.SaveChangesAsync();

        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.SaveFileAsync(
                contract.Id,
                3, // Expected next version: 2 + 1 = 3
                "file_v3.pdf",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync($"storage/contracts/{contract.Id}/v3_file_v3.pdf");

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.UserId).Returns(uploaderId);

        var handler = new UploadAttachmentCommandHandler(context, mockStorage.Object, mockCurrentUser.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Version 3 content"));
        var command = new UploadAttachmentCommand
        {
            ContractId = contract.Id,
            FileName = "file_v3.pdf",
            FileStream = stream
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be(3);
        result.FileUrl.Should().Be($"storage/contracts/{contract.Id}/v3_file_v3.pdf");

        var inDb = await context.Attachments.FirstOrDefaultAsync(a => a.Id == result.Id);
        inDb.Should().NotBeNull();
        inDb!.Version.Should().Be(3);
    }

    [Fact]
    public async Task UploadAttachment_WhenContractNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockStorage = new Mock<IStorageService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        var handler = new UploadAttachmentCommandHandler(context, mockStorage.Object, mockCurrentUser.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Content"));
        var command = new UploadAttachmentCommand
        {
            ContractId = Guid.NewGuid(), // Non-existent
            FileName = "file.pdf",
            FileStream = stream
        };

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetAttachmentsByContract_ShouldReturnInDescendingVersionOrder()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var contractId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        context.Attachments.AddRange(
            new Attachment(Guid.NewGuid(), contractId, "version1.pdf", 1, "path1", uploaderId, DateTime.UtcNow.AddHours(-3)),
            new Attachment(Guid.NewGuid(), contractId, "version3.pdf", 3, "path3", uploaderId, DateTime.UtcNow.AddHours(-1)),
            new Attachment(Guid.NewGuid(), contractId, "version2.pdf", 2, "path2", uploaderId, DateTime.UtcNow.AddHours(-2))
        );
        await context.SaveChangesAsync();

        var handler = new GetAttachmentsByContractQueryHandler(context);

        // Act
        var results = await handler.Handle(new GetAttachmentsByContractQuery(contractId), CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3);
        results[0].Version.Should().Be(3);
        results[1].Version.Should().Be(2);
        results[2].Version.Should().Be(1);
    }

    [Fact]
    public async Task GetAttachmentForDownload_WhenExists_ShouldReturnStreamAndMetadata()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var attachmentId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var attachment = new Attachment(
            attachmentId,
            contractId,
            "hop_dong_chinh_thuc.pdf",
            1,
            "storage/contracts/path.pdf",
            Guid.NewGuid(),
            DateTime.UtcNow
        );
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        var testStream = new MemoryStream(Encoding.UTF8.GetBytes("PDF byte content"));
        var mockStorage = new Mock<IStorageService>();
        mockStorage.Setup(s => s.GetFileAsync(attachment.FileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testStream);

        var handler = new GetAttachmentForDownloadQueryHandler(context, mockStorage.Object);

        // Act
        var result = await handler.Handle(new GetAttachmentForDownloadQuery(attachmentId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("hop_dong_chinh_thuc.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.FileStream.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAttachmentForDownload_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockStorage = new Mock<IStorageService>();
        var handler = new GetAttachmentForDownloadQueryHandler(context, mockStorage.Object);

        // Act
        var result = await handler.Handle(new GetAttachmentForDownloadQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UploadAttachmentCommandValidator_WhenInvalid_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new UploadAttachmentCommandValidator();
        var invalidCommand = new UploadAttachmentCommand
        {
            ContractId = Guid.Empty,
            FileName = "",
            FileStream = null!
        };

        // Act
        var result = validator.Validate(invalidCommand);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.ContractId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.FileName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.FileStream));
    }
}
