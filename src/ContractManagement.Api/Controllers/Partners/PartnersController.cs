using ContractManagement.Application.Features.Partners;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Partners;

[ApiController]
[Route("api/v1/partners")]
[Tags("Partners")]
public class PartnersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PartnersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách đối tác có phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPartners([FromQuery] GetPartnersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin đối tác theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPartnerById(Guid id)
    {
        var query = new GetPartnerByIdQuery(id);
        var result = await _mediator.Send(query);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy đối tác với Id: {id}" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một đối tác
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePartner([FromBody] CreatePartnerCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPartnerById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Cập nhật thông tin đối tác
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePartner(Guid id, [FromBody] UpdatePartnerCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "Id trong URL và trong body không khớp." });
        }

        var result = await _mediator.Send(command);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy đối tác với Id: {id}" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Xóa đối tác
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePartner(Guid id)
    {
        var command = new DeletePartnerRequest(id);
        var result = await _mediator.Send(command);
        if (!result)
        {
            return NotFound(new { message = $"Không tìm thấy đối tác với Id: {id}" });
        }
        return NoContent();
    }
}