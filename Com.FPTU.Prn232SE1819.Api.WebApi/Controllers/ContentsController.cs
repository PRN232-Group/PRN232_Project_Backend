using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/contents")]
public class ContentsController : ControllerBase
{
    private readonly IContentService _contents;

    public ContentsController(IContentService contents) => _contents = contents;

    private bool IsAdmin =>
        User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IList<ContentDto>>> GetAll()
        => Ok(await _contents.GetAllAsync(publishedOnly: !IsAdmin));

    [HttpGet("/api/Blog")]
    [AllowAnonymous]
    public async Task<ActionResult<IList<ContentDto>>> GetBlogPosts()
    {
        var list = await _contents.GetAllAsync(publishedOnly: true);
        return Ok(list.Where(x => string.Equals(x.Type, "Blog", StringComparison.OrdinalIgnoreCase)).ToList());
    }

    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ContentDto>> GetBySlug(string slug)
    {
        var item = await _contents.GetBySlugAsync(slug);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ContentDto>> GetById(int id)
    {
        var item = await _contents.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!item.IsPublished && !IsAdmin) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentDto>> Create([FromBody] ContentUpsertDto dto)
    {
        try { return Ok(await _contents.CreateAsync(dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentDto>> Update(int id, [FromBody] ContentUpsertDto dto)
    {
        try { return Ok(await _contents.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _contents.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
