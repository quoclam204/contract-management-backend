using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Application.Features.Attachments;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Attachments
{
    [ApiController]
    [Tags("Attachments")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public AttachmentsController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
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
    }
}
