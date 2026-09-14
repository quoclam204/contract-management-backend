using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ContractManagement.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace ContractManagement.UnitTests.Attachments;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _testStorageDir;
    private readonly LocalStorageService _storageService;

    public LocalStorageServiceTests()
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "storage_test_" + Guid.NewGuid().ToString("N"));
        _storageService = new LocalStorageService(_testStorageDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStorageDir))
        {
            try
            {
                Directory.Delete(_testStorageDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in temp
            }
        }
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCreateDirectoryAndSaveContent()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var fileName = "test_contract.pdf";
        var version = 1;
        var fileContent = "Sample Contract Content 12345";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var relativePath = await _storageService.SaveFileAsync(contractId, version, fileName, stream);

        // Assert
        relativePath.Should().Be($"storage/contracts/{contractId}/v1_{fileName}");

        var expectedPhysicalFile = Path.Combine(_testStorageDir, "contracts", contractId.ToString(), $"v1_{fileName}");
        File.Exists(expectedPhysicalFile).Should().BeTrue();

        var readText = await File.ReadAllTextAsync(expectedPhysicalFile);
        readText.Should().Be(fileContent);
    }

    [Fact]
    public async Task GetFileAsync_WhenFileExists_ShouldReturnStream()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var fileName = "read_test.docx";
        var version = 2;
        var fileContent = "Read Test Content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var relativePath = await _storageService.SaveFileAsync(contractId, version, fileName, stream);

        // Act
        using var readStream = await _storageService.GetFileAsync(relativePath);
        using var reader = new StreamReader(readStream);
        var content = await reader.ReadToEndAsync();

        // Assert
        content.Should().Be(fileContent);
    }

    [Fact]
    public async Task GetFileAsync_WhenFileNotFound_ShouldThrowFileNotFoundException()
    {
        // Act
        var act = async () => await _storageService.GetFileAsync("storage/contracts/non_existent/v1_file.pdf");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileExists_ShouldDeleteFile()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var fileName = "delete_test.pdf";
        var version = 1;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("To be deleted"));
        var relativePath = await _storageService.SaveFileAsync(contractId, version, fileName, stream);

        // Act
        await _storageService.DeleteFileAsync(relativePath);

        // Assert
        var expectedPhysicalFile = Path.Combine(_testStorageDir, "contracts", contractId.ToString(), $"v1_{fileName}");
        File.Exists(expectedPhysicalFile).Should().BeFalse();
    }
}
