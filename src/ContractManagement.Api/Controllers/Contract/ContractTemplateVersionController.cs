using ContractManagement.Application.Contract.DTOs;
using ContractManagement.Application.Contract.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Contract;

[ApiController]
[Route("api/[controller]")]
[Tags("Contract Templates")]
public class ContractTemplateVersionController : ControllerBase
{
    private readonly IContractTemplateVersionService _contractTemplateVersionService;

    public ContractTemplateVersionController(IContractTemplateVersionService contractTemplateVersionService)
    {
        _contractTemplateVersionService = contractTemplateVersionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContractTemplateVersionDto>>> GetAll()
    {
        var contractTemplateVersions = await _contractTemplateVersionService.GetAllAsync();
        return Ok(contractTemplateVersions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContractTemplateVersionDto>> GetById(Guid id)
    {
        var contractTemplateVersion = await _contractTemplateVersionService.GetByIdAsync(id);
        if (contractTemplateVersion == null)
            return NotFound();

        return Ok(contractTemplateVersion);
    }

    [HttpPost]
    public async Task<ActionResult<ContractTemplateVersionDto>> Create([FromBody] ContractTemplateVersionDto dto)
    {
        var createdContractTemplateVersion = await _contractTemplateVersionService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = createdContractTemplateVersion.Id }, createdContractTemplateVersion);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContractTemplateVersionDto>> Update(Guid id, [FromBody] ContractTemplateVersionDto dto)
    {
        if (id != dto.Id)
            return BadRequest("Id mismatch");

        var updatedContractTemplateVersion = await _contractTemplateVersionService.UpdateAsync(id, dto);
        if (updatedContractTemplateVersion == null)
            return NotFound();

        return Ok(updatedContractTemplateVersion);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        var result = await _contractTemplateVersionService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return Ok(result);
    }
}
