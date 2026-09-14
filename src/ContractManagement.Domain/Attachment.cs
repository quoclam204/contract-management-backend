using System;

namespace ContractManagement.Domain
{
    /// <summary>
    /// Bảng ATTACHMENTS: Lưu trữ thông tin tệp đính kèm và kiểm soát phiên bản (SRS v3 - Mục 4.11, FR-07)
    /// </summary>
    public class Attachment
    {
        public Guid Id { get; private set; }
        public Guid ContractId { get; private set; }
        public string FileName { get; private set; } = default!;
        public int Version { get; private set; }
        public string FileUrl { get; private set; } = default!;
        public Guid UploadedBy { get; private set; }
        public DateTime UploadedAt { get; private set; }

        // Constructor for creating a new attachment
        public Attachment(Guid id, Guid contractId, string fileName, int version, string fileUrl, Guid uploadedBy, DateTime uploadedAt)
        {
            Id = id;
            ContractId = contractId;
            FileName = fileName;
            Version = version;
            FileUrl = fileUrl;
            UploadedBy = uploadedBy;
            UploadedAt = uploadedAt;
        }

        // Constructor for EF Core
        protected Attachment() { }
    }
}
