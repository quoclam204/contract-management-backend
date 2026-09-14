using ContractManagement.Infrastructure.Storage;
using Xunit;

namespace ContractManagement.UnitTests.Infrastructure.Storage;

public class LocalStorageProviderTests : IDisposable
{
    private readonly string _testStoragePath;
    private readonly LocalStorageProvider _provider;

    public LocalStorageProviderTests()
    {
        // Use a unique temporary directory for each test
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"storage_test_{Guid.NewGuid()}");
        _provider = new LocalStorageProvider(_testStoragePath);
    }

    public void Dispose()
    {
        // Clean up test directory after each test
        try
        {
            if (Directory.Exists(_testStoragePath))
            {
                Directory.Delete(_testStoragePath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Upload and Download Tests

    [Fact]
    public async Task UploadFileAsync_WithValidContent_StoresFile()
    {
        // Arrange
        var fileName = "test-file.txt";
        var content = "test content"u8.ToArray();

        // Act
        var result = await _provider.UploadFileAsync(fileName, content);

        // Assert
        Assert.Equal(fileName, result);
        Assert.True(File.Exists(Path.Combine(_testStoragePath, fileName)));
    }

    [Fact]
    public async Task UploadThenDownload_ReturnsSameContent()
    {
        // Arrange
        var fileName = "test-document.bin";
        var originalContent = new byte[] { 1, 2, 3, 4, 5, 255, 254, 253 };

        // Act
        await _provider.UploadFileAsync(fileName, originalContent);
        var downloadedContent = await _provider.DownloadFileAsync(fileName);

        // Assert
        Assert.Equal(originalContent, downloadedContent);
    }

    [Fact]
    public async Task DownloadFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _provider.DownloadFileAsync("non-existent-file.txt"));
    }

    [Fact]
    public async Task UploadFileAsync_WithEmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.UploadFileAsync("file.txt", Array.Empty<byte>()));
    }

    [Fact]
    public async Task UploadFileAsync_WithNullContent_ThrowsArgumentException()
    {
        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.UploadFileAsync("file.txt", null));
#pragma warning restore CS8625
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_RemovesFile()
    {
        // Arrange
        var fileName = "file-to-delete.txt";
        var content = "content"u8.ToArray();
        await _provider.UploadFileAsync(fileName, content);

        var filePath = Path.Combine(_testStoragePath, fileName);
        Assert.True(File.Exists(filePath));

        // Act
        await _provider.DeleteFileAsync(fileName);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _provider.DeleteFileAsync("non-existent-file.txt");
    }

    #endregion

    #region Path Traversal Prevention Tests

    [Fact]
    public async Task UploadFileAsync_WithPathTraversalAttempt_PreventsEscape()
    {
        // Arrange
        var maliciousFileName = "../../malicious.txt";
        var content = "malicious"u8.ToArray();

        // Act
        var result = await _provider.UploadFileAsync(maliciousFileName, content);

        // Assert
        // The file should be stored in the storage root, not outside it
        var storedPath = Path.Combine(_testStoragePath, result);
        Assert.True(storedPath.StartsWith(_testStoragePath, StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(storedPath));

        // Verify the parent directory doesn't contain the file
        var parentDir = Directory.GetParent(_testStoragePath)?.FullName;
        if (parentDir != null)
        {
            Assert.False(File.Exists(Path.Combine(parentDir, "malicious.txt")));
        }
    }

    [Fact]
    public async Task DownloadFileAsync_WithPathTraversalAttempt_PreventsEscape()
    {
        // Arrange
        var maliciousPath = "../../etc/passwd";

        // Act & Assert
        // When path traversal is prevented, the sanitized path won't exist
        // So it should throw FileNotFoundException (file not found after sanitization)
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _provider.DownloadFileAsync(maliciousPath));
    }

    [Fact]
    public async Task UploadFileAsync_WithBackslashTraversal_SanitizesCorrectly()
    {
        // Arrange
        var fileName = "subdir\\..\\..\\escape.txt";
        var content = "test"u8.ToArray();

        // Act
        var result = await _provider.UploadFileAsync(fileName, content);

        // Assert
        var storedPath = Path.Combine(_testStoragePath, result);
        Assert.True(storedPath.StartsWith(_testStoragePath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UploadFileAsync_WithColonInFileName_Sanitizes()
    {
        // Arrange
        var fileName = "file:with:colons.txt";
        var content = "test"u8.ToArray();

        // Act
        var result = await _provider.UploadFileAsync(fileName, content);

        // Assert
        // Colons should be sanitized
        Assert.DoesNotContain(":", result);
        Assert.True(File.Exists(Path.Combine(_testStoragePath, result)));
    }

    #endregion

    #region GetPresignedUrl Tests

    [Fact]
    public async Task GetPresignedUrlAsync_WithExistingFile_ReturnsRelativePath()
    {
        // Arrange
        var fileName = "document.pdf";
        var content = new byte[] { 1, 2, 3 };
        await _provider.UploadFileAsync(fileName, content);

        // Act
        var url = await _provider.GetPresignedUrlAsync(fileName);

        // Assert
        Assert.NotNull(url);
        Assert.Equal(fileName, url);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _provider.GetPresignedUrlAsync("non-existent.pdf"));
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WithExpirationParameter_ReturnsUrl()
    {
        // Arrange
        var fileName = "file.txt";
        var content = "content"u8.ToArray();
        await _provider.UploadFileAsync(fileName, content);

        // Act
        var url = await _provider.GetPresignedUrlAsync(fileName, expirationInMinutes: 120);

        // Assert
        Assert.NotNull(url);
        Assert.Equal(fileName, url);
    }

    #endregion

    #region Directory Creation Tests

    [Fact]
    public void Constructor_WithNonExistentPath_CreatesDirectory()
    {
        // Arrange
        var newPath = Path.Combine(Path.GetTempPath(), $"new_storage_{Guid.NewGuid()}");
        Assert.False(Directory.Exists(newPath));

        try
        {
            // Act
            var provider = new LocalStorageProvider(newPath);

            // Assert
            Assert.True(Directory.Exists(newPath));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(newPath))
            {
                Directory.Delete(newPath, recursive: true);
            }
        }
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task UploadFileAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var content = "test"u8.ToArray();

        // Act & Assert
        // File I/O operations throw TaskCanceledException (subclass of OperationCanceledException)
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _provider.UploadFileAsync("file.txt", content, cts.Token));
    }

    [Fact]
    public async Task DownloadFileAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        var fileName = "file.txt";
        var content = "test"u8.ToArray();
        await _provider.UploadFileAsync(fileName, content);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // File I/O operations throw TaskCanceledException (subclass of OperationCanceledException)
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _provider.DownloadFileAsync(fileName, cts.Token));
    }

    [Fact]
    public async Task DeleteFileAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var fileName = "file.txt";
        var content = "test"u8.ToArray();
        await _provider.UploadFileAsync(fileName, content);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // Manual cancellation check throws OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _provider.DeleteFileAsync(fileName, cts.Token));
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task UploadFileAsync_WithNullFileName_ThrowsArgumentException()
    {
        // Act & Assert
#pragma warning disable CS8625
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.UploadFileAsync(null, new byte[] { 1 }));
#pragma warning restore CS8625
    }

    [Fact]
    public async Task UploadFileAsync_WithEmptyFileName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.UploadFileAsync("", new byte[] { 1 }));
    }

    [Fact]
    public async Task DownloadFileAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
#pragma warning disable CS8625
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.DownloadFileAsync(null));
#pragma warning restore CS8625
    }

    [Fact]
    public async Task DeleteFileAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
#pragma warning disable CS8625
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.DeleteFileAsync(null));
#pragma warning restore CS8625
    }

    #endregion
}
