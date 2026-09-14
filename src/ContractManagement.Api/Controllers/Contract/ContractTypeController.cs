using ContractManagement.Application.Contract.DTOs;
using ContractManagement.Application.Contract.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers.Contract;

[ApiController]
[Route("api/[controller]")]
[Tags("Contract Types")]
public class ContractTypeController : ControllerBase
{
    private readonly IContractTypeService _contractTypeService;

    public ContractTypeController(IContractTypeService contractTypeService)
    {
        _contractTypeService = contractTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContractTypeDto>>> GetAll()
    {
        var contractTypes = await _contractTypeService.GetAllAsync();
        return Ok(contractTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContractTypeDto>> GetById(Guid id)
    {
        var contractType = await _contractTypeService.GetByIdAsync(id);
        if (contractType == null)
            return NotFound();

        return Ok(contractType);
    }

    [HttpPost]
    public async Task<ActionResult<ContractTypeDto>> Create([FromBody] ContractTypeDto dto)
    {
        var createdContractType = await _contractTypeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = createdContractType.Id }, createdContractType);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContractTypeDto>> Update(Guid id, [FromBody] ContractTypeDto dto)
    {
        if (id != dto.Id)
            return BadRequest("Id mismatch");

        var updatedContractType = await _contractTypeService.UpdateAsync(id, dto);
        if (updatedContractType == null)
            return NotFound();

        return Ok(updatedContractType);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        var result = await _contractTypeService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return Ok(result);
    }
}
