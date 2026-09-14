# Attachment & File Versioning Module Rules (Người 3)

## Scope of Work for Người 3 (Attachment & Versioning)
- **Attachment Entity & Versioning**: Tạo và quản lý tệp đính kèm theo từng hợp đồng (`ContractId`), tự động tăng phiên bản (`Version = 1` cho tệp đầu tiên, `Version = N + 1` cho các tệp tiếp theo).
- **IStorageService Abstraction**: Định nghĩa interface dịch vụ lưu trữ tệp phục vụ lưu, đọc và xóa file.
- **LocalStorageService (MVP)**: Triển khai lưu trữ vật lý trực tiếp trên local disk tại thư mục `./storage/contracts/{contractId}/v{version}_{fileName}`.
- **Attachment Endpoints**: Endpoint tải lên tệp (multipart/form-data), xem lịch sử các phiên bản tệp của hợp đồng, và tải tệp tin về máy (download stream).

## Architectural Guidelines
- Tuân thủ nghiêm ngặt Clean Architecture theo `.claude/rules/architecture.md`.
- Tuân thủ coding convention theo `.claude/rules/coding.md`.
- Đảm bảo tính trung thực 100% với `database.sql` (bảng `dbo.ATTACHMENTS`).
- Sử dụng mô hình CQRS qua MediatR, FluentValidation, và DbContext interface (`IAttachmentDbContext`) theo đúng Modular Monolith.

## Domain Layer (`ContractManagement.Domain`)
- Entity `Attachment.cs`: Pure POCO không phụ thuộc tầng ngoài.
- Thuộc tính:
  - `Id`: `Guid` (PK)
  - `ContractId`: `Guid` (FK trỏ tới `Contracts`)
  - `FileName`: `string` (Tên file gốc, tối đa 500 ký tự)
  - `Version`: `int` (Số phiên bản tự tăng: 1, 2, 3...)
  - `FileUrl`: `string` (Đường dẫn tương đối: `storage/contracts/{contractId}/v{version}_{fileName}`)
  - `UploadedBy`: `Guid` (FK trỏ tới `Users`)
  - `UploadedAt`: `DateTime` (UTC timestamp)

## Application Layer (`ContractManagement.Application`)
- **Interfaces (`Common/Interfaces`)**:
  - `IStorageService`: `SaveFileAsync`, `GetFileAsync`, `DeleteFileAsync`.
  - `IAttachmentDbContext`: Khai báo `DbSet<Attachment>`, `DbSet<Contract>`, `SaveChangesAsync`.
- **Features (`Features/Attachments`)**:
  - `UploadAttachmentCommand` & `UploadAttachmentCommandHandler`: Kiểm tra hợp đồng tồn tại, tính toán version tự tăng `Max(Version) + 1`, lưu file vật lý qua `IStorageService`, ghi nhận `Attachment` vào DB.
  - `UploadAttachmentCommandValidator`: Kiểm tra `ContractId`, `FileName`, `FileStream`.
  - `GetAttachmentsByContractQuery` & `GetAttachmentsByContractQueryHandler`: Lấy danh sách phiên bản tệp sắp xếp `Version` giảm dần.
  - `GetAttachmentForDownloadQuery` & `GetAttachmentForDownloadQueryHandler`: Truy xuất metadata và stream tệp vật lý để tải về.
  - DTOs: `AttachmentDto`, `AttachmentDownloadDto`.

## Infrastructure Layer (`ContractManagement.Infrastructure`)
- **EF Core Configuration (`Persistence/Configurations/Attachments/AttachmentConfiguration.cs`)**:
  - Khớp 100% với bảng `dbo.ATTACHMENTS` trong `database.sql`.
  - Bảng: `ATTACHMENTS`.
  - Khóa chính: `Id` (`uniqueidentifier`, default `NEWID()`).
  - Unique Constraint: `UQ_ATTACHMENTS_ContractFileVersion` trên `(ContractId, FileName, Version)`.
  - Index: `IX_ATTACHMENTS_ContractId`.
  - Khóa ngoại: `FK_ATTACHMENTS_CONTRACTS` (Cascade delete), `FK_ATTACHMENTS_USERS`.
- **Persistence (`Persistence/ContractManagementDbContext.cs`)**:
  - Implement `IAttachmentDbContext`, đăng ký `DbSet<Attachment> Attachments`.
- **Storage Implementation (`Services/LocalStorageService.cs`)**:
  - Lưu tệp tin tại `./storage/contracts/{contractId}/v{version}_{fileName}`.
  - Tự động tạo thư mục bằng `Directory.CreateDirectory` nếu chưa tồn tại.
- **Dependency Injection (`InfrastructureServiceCollectionExtensions.cs`)**:
  - Đăng ký `services.AddScoped<IStorageService, LocalStorageService>()`.

## Presentation Layer (`ContractManagement.Api`)
- `AttachmentsController.cs`:
  - `POST /api/v1/contracts/{contractId}/attachments`: Upload tệp đính kèm (`multipart/form-data`).
  - `GET /api/v1/contracts/{contractId}/attachments`: Danh sách các phiên bản tệp đính kèm theo hợp đồng.
  - `GET /api/v1/attachments/{id}/download`: Tải tệp đính kèm về client (Stream Result).

## Testing & Quality Assurance
- **Unit Tests (`tests/ContractManagement.UnitTests/Attachments/`)**:
  - `AttachmentHandlerTests.cs`: Kiểm tra logic versioning (bắt đầu = 1, tăng dần = N + 1), query sắp xếp giảm dần, download stream, validation.
  - `LocalStorageServiceTests.cs`: Kiểm tra lưu tệp, tạo thư mục tự động, đọc tệp stream, xóa tệp.
  - `AttachmentsControllerTests.cs`: Kiểm tra các HTTP action results và validation ở tầng controller.
  - `ArchitectureTests.cs`: Đảm bảo quy chuẩn NetArchTest Clean Architecture không bị vi phạm.
- **Test Results**: 100% passed (110/110 tests).

## Reminders
- `database.sql` là nguồn chân lý duy nhất (Single Source of Truth) cho cơ sở dữ liệu.
- Không tự ý thêm bớt cột, bảng ngoài `database.sql`.
- Quản lý tệp phải luôn thông qua interface `IStorageService` abstraction để có thể dễ dàng chuyển đổi sang MinIO/S3/Azure Blob Storage trong tương lai.
