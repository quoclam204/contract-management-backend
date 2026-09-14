using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ContractManagement.Infrastructure.Services
{
    /// <summary>
    /// Triển khai IStorageService lưu trữ tệp cục bộ trên máy chủ (MVP Local Storage)
    /// </summary>
    public class LocalStorageService : IStorageService
    {
        private readonly string _baseDirectory;

        public LocalStorageService(IConfiguration? configuration = null)
        {
            var configuredPath = configuration?["Storage:BasePath"];
            _baseDirectory = !string.IsNullOrWhiteSpace(configuredPath)
                ? configuredPath
                : Path.Combine(Directory.GetCurrentDirectory(), "storage");
        }

        public LocalStorageService(string baseDirectory)
        {
            _baseDirectory = baseDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        }

        public async Task<string> SaveFileAsync(Guid contractId, int version, string fileName, Stream fileStream, CancellationToken ct = default)
        {
            // Định dạng đường dẫn tương đối lưu vào DB: storage/contracts/{contractId}/v{version}_{fileName}
            var relativePath = $"storage/contracts/{contractId}/v{version}_{fileName}";

            // Thư mục vật lý: {baseDirectory}/contracts/{contractId}
            var contractDirectory = Path.Combine(_baseDirectory, "contracts", contractId.ToString());
            if (!Directory.Exists(contractDirectory))
            {
                Directory.CreateDirectory(contractDirectory);
            }

            var fullPath = Path.Combine(contractDirectory, $"v{version}_{fileName}");

            if (fileStream.CanSeek && fileStream.Position > 0)
            {
                fileStream.Position = 0;
            }

            using (var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await fileStream.CopyToAsync(outputStream, ct);
            }

            return relativePath;
        }

        public Task<Stream> GetFileAsync(string relativePath, CancellationToken ct = default)
        {
            var normalizedRelative = relativePath.Replace('\\', '/');
            if (normalizedRelative.StartsWith("storage/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRelative = normalizedRelative.Substring("storage/".Length);
            }

            var fullPath = Path.Combine(_baseDirectory, normalizedRelative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                var directPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(directPath))
                {
                    fullPath = directPath;
                }
                else
                {
                    throw new FileNotFoundException($"Không tìm thấy tệp tin tại đường dẫn: {relativePath}");
                }
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            return Task.FromResult(stream);
        }

        public Task DeleteFileAsync(string relativePath, CancellationToken ct = default)
        {
            var normalizedRelative = relativePath.Replace('\\', '/');
            if (normalizedRelative.StartsWith("storage/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRelative = normalizedRelative.Substring("storage/".Length);
            }

            var fullPath = Path.Combine(_baseDirectory, normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else
            {
                var directPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(directPath))
                {
                    File.Delete(directPath);
                }
            }

            return Task.CompletedTask;
        }
    }
}
