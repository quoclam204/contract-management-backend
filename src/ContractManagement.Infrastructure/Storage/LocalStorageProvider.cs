using ContractManagement.Application.Common.Interfaces;

namespace ContractManagement.Infrastructure.Storage;

/// <summary>
/// Local file system implementation of IStorageProvider.
/// Stores files on the local disk in a configured root directory.
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly string _storagePath;

    public LocalStorageProvider(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be null or empty.", nameof(storagePath));

        _storagePath = Path.GetFullPath(storagePath);

        // Ensure the storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    /// <summary>
    /// Upload a file to local storage.
    /// </summary>
    public async Task<string> UploadFileAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (content == null || content.Length == 0)
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));

        // Prevent path traversal attacks
        var sanitizedFileName = SanitizeFileName(fileName);
        var filePath = Path.Combine(_storagePath, sanitizedFileName);

        // Ensure the resolved path is still within the storage root
        if (!Path.GetFullPath(filePath).StartsWith(_storagePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File path resolves outside the storage root directory.");

        try
        {
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);
            return sanitizedFileName;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to upload file '{fileName}'.", ex);
        }
    }

    /// <summary>
    /// Download a file from local storage.
    /// </summary>
    public async Task<byte[]> DownloadFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var sanitizedPath = SanitizeFileName(filePath);
        var fullPath = Path.Combine(_storagePath, sanitizedPath);

        // Prevent path traversal attacks
        if (!Path.GetFullPath(fullPath).StartsWith(_storagePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File path resolves outside the storage root directory.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: '{filePath}'");

        try
        {
            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to download file '{filePath}'.", ex);
        }
    }

    /// <summary>
    /// Get a presigned URL for a file.
    /// For local storage, this returns a relative path that represents the file.
    /// In a real implementation with object storage (S3, Azure), this would return a time-limited signed URL.
    /// </summary>
    public async Task<string> GetPresignedUrlAsync(
        string filePath,
        int expirationInMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var sanitizedPath = SanitizeFileName(filePath);
        var fullPath = Path.Combine(_storagePath, sanitizedPath);

        // Prevent path traversal attacks
        if (!Path.GetFullPath(fullPath).StartsWith(_storagePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File path resolves outside the storage root directory.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: '{filePath}'");

        // For local storage, return the relative file path
        // In production with object storage, this would return a signed URL with expiration
        return await Task.FromResult(sanitizedPath);
    }

    /// <summary>
    /// Delete a file from local storage.
    /// </summary>
    public async Task DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var sanitizedPath = SanitizeFileName(filePath);
        var fullPath = Path.Combine(_storagePath, sanitizedPath);

        // Prevent path traversal attacks
        if (!Path.GetFullPath(fullPath).StartsWith(_storagePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File path resolves outside the storage root directory.");

        // Check cancellation before attempting file operations
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(fullPath))
            return; // Idempotent: no error if file doesn't exist

        try
        {
            File.Delete(fullPath);
            await Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to delete file '{filePath}'.", ex);
        }
    }

    /// <summary>
    /// Sanitize a file name to prevent path traversal attacks.
    /// Removes path separators and special characters that could be used for traversal.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Replace path separators with safe character
        var sanitized = fileName
            .Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_')
            .Replace(":", "_")
            .Replace("..", "");

        // Remove leading dots to prevent hidden files and parent directory references
        while (sanitized.StartsWith("."))
        {
            sanitized = sanitized.Substring(1);
        }

        return sanitized;
    }
}
