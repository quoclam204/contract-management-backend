using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Infrastructure.Services
{
    /// <summary>
    /// Triển khai IStorageService lưu trữ tệp trực tiếp vào bảng AttachmentBlobs trong SQL Server
    /// Giúp toàn bộ thành viên trong nhóm và server deploy dùng chung kho file mà không bị lệch file cục bộ.
    /// </summary>
    public class DatabaseStorageService : IStorageService
    {
        private readonly string _connectionString;
        private readonly string _localBaseDirectory;
        private readonly ILogger<DatabaseStorageService>? _logger;

        public DatabaseStorageService(IConfiguration configuration, ILogger<DatabaseStorageService>? logger = null)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' không tồn tại.");
            
            var configuredPath = configuration["Storage:LocalPath"] ?? configuration["Storage:BasePath"];
            _localBaseDirectory = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.Combine(Directory.GetCurrentDirectory(), "storage");

            _logger = logger;
        }

        public async Task<string> SaveFileAsync(Guid contractId, int version, string fileName, Stream fileStream, CancellationToken ct = default)
        {
            var relativePath = $"storage/contracts/{contractId}/v{version}_{fileName}";

            // Đọc stream thành byte[]
            byte[] fileBytes;
            if (fileStream.CanSeek && fileStream.Position > 0)
            {
                fileStream.Position = 0;
            }

            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream, ct);
                fileBytes = memoryStream.ToArray();
            }

            // 1. Lưu vào bảng AttachmentBlobs trong SQL Server
            await SaveToDatabaseAsync(relativePath, fileBytes, ct);

            // 2. Đồng thời lưu một bản dự phòng tại local disk (nếu thư mục có thể ghi)
            try
            {
                var contractDir = Path.Combine(_localBaseDirectory, "contracts", contractId.ToString());
                if (!Directory.Exists(contractDir))
                {
                    Directory.CreateDirectory(contractDir);
                }
                var localFilePath = Path.Combine(contractDir, $"v{version}_{fileName}");
                await File.WriteAllBytesAsync(localFilePath, fileBytes, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Không thể lưu bản copy cục bộ tại đĩa, nhưng đã lưu thành công vào Database: {Path}", relativePath);
            }

            return relativePath;
        }

        public async Task<Stream> GetFileAsync(string relativePath, CancellationToken ct = default)
        {
            var normalized = NormalizePath(relativePath);

            // 1. Tìm trong SQL Server AttachmentBlobs
            var fileBytes = await GetFromDatabaseAsync(normalized, ct);
            if (fileBytes != null && fileBytes.Length > 0)
            {
                return new MemoryStream(fileBytes);
            }

            // 2. Fallback: Nếu trong DB chưa có (do file cũ từ local), tìm tại ổ đĩa local
            var localFile = FindLocalFile(normalized);
            if (localFile != null && File.Exists(localFile))
            {
                var diskBytes = await File.ReadAllBytesAsync(localFile, ct);
                // Tự động đẩy file cũ này lên Database để lần sau các máy khác cũng tải được!
                _ = Task.Run(async () =>
                {
                    try { await SaveToDatabaseAsync(normalized, diskBytes, CancellationToken.None); }
                    catch { /* bỏ qua lỗi nền */ }
                });

                return new MemoryStream(diskBytes);
            }

            throw new FileNotFoundException($"Không tìm thấy tệp tin: {relativePath}");
        }

        public async Task DeleteFileAsync(string relativePath, CancellationToken ct = default)
        {
            var normalized = NormalizePath(relativePath);

            // 1. Xóa trong database
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var sql = "DELETE FROM AttachmentBlobs WHERE FilePath = @FilePath OR FilePath = @AltPath";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 400).Value = normalized;
            cmd.Parameters.Add("@AltPath", SqlDbType.NVarChar, 400).Value = StripStoragePrefix(normalized);

            await cmd.ExecuteNonQueryAsync(ct);

            // 2. Xóa trên đĩa cục bộ nếu có
            var localFile = FindLocalFile(normalized);
            if (localFile != null && File.Exists(localFile))
            {
                try { File.Delete(localFile); } catch { }
            }
        }

        private async Task SaveToDatabaseAsync(string filePath, byte[] content, CancellationToken ct)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            // MERGE để Update nếu đã có, Insert nếu chưa có
            var sql = @"
                MERGE INTO AttachmentBlobs AS target
                USING (SELECT @FilePath AS FilePath) AS source
                ON target.FilePath = source.FilePath
                WHEN MATCHED THEN
                    UPDATE SET Content = @Content, CreatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (FilePath, Content, CreatedAt)
                    VALUES (@FilePath, @Content, SYSUTCDATETIME());";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 400).Value = filePath;
            cmd.Parameters.Add("@Content", SqlDbType.VarBinary, -1).Value = content;

            await cmd.ExecuteNonQueryAsync(ct);
        }

        private async Task<byte[]?> GetFromDatabaseAsync(string filePath, CancellationToken ct)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var sql = @"
                SELECT TOP 1 Content 
                FROM AttachmentBlobs 
                WHERE FilePath = @FilePath OR FilePath = @AltPath";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 400).Value = filePath;
            cmd.Parameters.Add("@AltPath", SqlDbType.NVarChar, 400).Value = StripStoragePrefix(filePath);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result as byte[];
        }

        private string? FindLocalFile(string relativePath)
        {
            var stripped = StripStoragePrefix(relativePath).Replace('/', Path.DirectorySeparatorChar);
            var pathInBase = Path.Combine(_localBaseDirectory, stripped);
            if (File.Exists(pathInBase)) return pathInBase;

            var direct = Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(direct)) return direct;

            return null;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string StripStoragePrefix(string path)
        {
            var normalized = NormalizePath(path);
            if (normalized.StartsWith("storage/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring("storage/".Length);
            }
            return normalized;
        }
    }
}
