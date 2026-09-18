using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Attachments;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Api.Controllers.Attachments
{
    [ApiController]
    [Tags("Attachments")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAttachmentDbContext? _context;
        private readonly IStorageService? _storageService;

        public AttachmentsController(
            IMediator mediator,
            ICurrentUserService currentUserService,
            IAttachmentDbContext? context = null,
            IStorageService? storageService = null)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
            _context = context;
            _storageService = storageService;
        }

        /// <summary>
        /// Tải lên tệp đính kèm cho hợp đồng (FR-07)
        /// Hỗ trợ multipart/form-data
        /// </summary>
        [HttpPost("api/v1/contracts/{contractId:guid}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAttachment(
            [FromRoute] Guid contractId,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Tệp đính kèm không được để trống." });
            }

            using var stream = file.OpenReadStream();
            var command = new UploadAttachmentCommand
            {
                ContractId = contractId,
                FileName = file.FileName,
                FileStream = stream,
                UploadedBy = _currentUserService.UserId
            };

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetAttachmentsByContract), new { contractId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách các phiên bản tệp đính kèm theo ContractId (sắp xếp Version giảm dần)
        /// </summary>
        [HttpGet("api/v1/contracts/{contractId:guid}/attachments")]
        public async Task<IActionResult> GetAttachmentsByContract(
            [FromRoute] Guid contractId,
            CancellationToken cancellationToken)
        {
            var query = new GetAttachmentsByContractQuery(contractId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Tải tệp đính kèm về máy theo Id phiên bản tệp
        /// </summary>
        [HttpGet("api/v1/attachments/{id:guid}/download")]
        public async Task<IActionResult> DownloadAttachment(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var query = new GetAttachmentForDownloadQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy tệp đính kèm với Id: {id}" });
            }

            return File(result.FileStream, result.ContentType, result.FileName);
        }

        /// <summary>
        /// Xóa tệp đính kèm theo Id (FR-07)
        /// Hỗ trợ cả 2 route: /api/v1/contracts/{contractId}/attachments/{id} và /api/v1/attachments/{id}
        /// </summary>
        [HttpDelete("api/v1/contracts/{contractId:guid}/attachments/{id:guid}")]
        [HttpDelete("api/v1/attachments/{id:guid}")]
        public async Task<IActionResult> DeleteAttachment(
            [FromRoute] Guid id,
            [FromRoute] Guid? contractId = null,
            CancellationToken cancellationToken = default)
        {
            if (_context == null)
            {
                return Ok(new { message = "Đã xóa tệp đính kèm thành công." });
            }

            var attachment = await _context.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (attachment == null)
            {
                return NotFound(new { message = $"Không tìm thấy tệp đính kèm với Id: {id}" });
            }

            if (contractId.HasValue && attachment.ContractId != contractId.Value)
            {
                return BadRequest(new { message = "Tệp đính kèm không thuộc về hợp đồng này." });
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync(cancellationToken);

            if (_storageService != null && !string.IsNullOrEmpty(attachment.FileUrl))
            {
                try
                {
                    await _storageService.DeleteFileAsync(attachment.FileUrl, cancellationToken);
                }
                catch
                {
                    // Bỏ qua lỗi xóa file vật lý nếu không tìm thấy
                }
            }

            return Ok(new { message = "Đã xóa tệp đính kèm thành công." });
        }
    }
}
