using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/interior-designs")]
public class InteriorDesignsController : ControllerBase
{
    private readonly IInteriorDesignService _designs;

    public InteriorDesignsController(IInteriorDesignService designs) => _designs = designs;

    private bool IsStaff =>
        User.Identity?.IsAuthenticated == true
        && (User.IsInRole("Manager") || User.IsInRole("Admin"));

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IList<InteriorDesignDetailDto>>> GetAll()
        => Ok(await _designs.GetAllAsync(includeUnpublished: IsStaff));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<InteriorDesignDetailDto>> GetById(int id)
    {
        var item = await _designs.GetByIdAsync(id, includeUnpublished: IsStaff);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<InteriorDesignDetailDto>> Create([FromBody] InteriorDesignUpsertDto dto)
    {
        try { return Ok(await _designs.CreateAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<InteriorDesignDetailDto>> Update(int id, [FromBody] InteriorDesignUpsertDto dto)
    {
        try { return Ok(await _designs.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _designs.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
