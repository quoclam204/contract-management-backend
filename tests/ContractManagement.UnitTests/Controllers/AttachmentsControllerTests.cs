using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Api.Controllers.Attachments;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Attachments;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ContractManagement.UnitTests.Controllers;

public class AttachmentsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly AttachmentsController _controller;

    public AttachmentsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _controller = new AttachmentsController(_mockMediator.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task UploadAttachment_WhenFileIsNull_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadAttachment(Guid.NewGuid(), null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadAttachment_WhenFileIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.UploadAttachment(Guid.NewGuid(), mockFile.Object, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadAttachment_WhenValid_ReturnsCreatedAtAction()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var mockFile = new Mock<IFormFile>();
        var content = "Sample file content";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        mockFile.Setup(f => f.Length).Returns(ms.Length);
        mockFile.Setup(f => f.FileName).Returns("contract.pdf");
        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

        var expectedDto = new AttachmentDto
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            FileName = "contract.pdf",
            Version = 1,
            FileUrl = $"storage/contracts/{contractId}/v1_contract.pdf",
            UploadedBy = Guid.NewGuid(),
            UploadedAt = DateTime.UtcNow
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<UploadAttachmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.UploadAttachment(contractId, mockFile.Object, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetAttachmentsByContract_ReturnsOkWithList()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var expectedList = new List<AttachmentDto>
        {
            new() { Id = Guid.NewGuid(), ContractId = contractId, FileName = "v2.pdf", Version = 2 },
            new() { Id = Guid.NewGuid(), ContractId = contractId, FileName = "v1.pdf", Version = 1 }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAttachmentsByContractQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await _controller.GetAttachmentsByContract(contractId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedList);
    }

    [Fact]
    public async Task DownloadAttachment_WhenFound_ReturnsFileStreamResult()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("File binary data"));
        var downloadDto = new AttachmentDownloadDto
        {
            FileStream = stream,
            FileName = "document.pdf",
            ContentType = "application/pdf"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAttachmentForDownloadQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadDto);

        // Act
        var result = await _controller.DownloadAttachment(attachmentId, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.FileDownloadName.Should().Be("document.pdf");
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileStream.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task DownloadAttachment_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAttachmentForDownloadQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttachmentDownloadDto?)null);

        // Act
        var result = await _controller.DownloadAttachment(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
