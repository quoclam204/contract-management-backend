using System;
using System.Threading;
using System.Threading.Tasks;

namespace ContractManagement.Application.Common.Interfaces
{
    public interface IStorageProvider
    {
        Task<string> UploadFileAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
        Task<byte[]> DownloadFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<string> GetPresignedUrlAsync(string filePath, int expirationInMinutes = 60, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}