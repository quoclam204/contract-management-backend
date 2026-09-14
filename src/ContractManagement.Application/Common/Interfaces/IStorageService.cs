using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ContractManagement.Application.Common.Interfaces
{
    /// <summary>
    /// Abstraction cho lưu trữ tệp đính kèm hợp đồng (FR-07)
    /// </summary>
    public interface IStorageService
    {
        Task<string> SaveFileAsync(Guid contractId, int version, string fileName, Stream fileStream, CancellationToken ct = default);
        Task<Stream> GetFileAsync(string relativePath, CancellationToken ct = default);
        Task DeleteFileAsync(string relativePath, CancellationToken ct = default);
    }
}
