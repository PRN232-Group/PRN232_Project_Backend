using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IList<CategoryDto>>> GetAll()
        => Ok(await _categories.GetDtosAsync());

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryUpsertDto dto)
        => Ok(await _categories.CreateDtoAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryUpsertDto dto)
    {
        try { return Ok(await _categories.UpdateDtoAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _categories.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
