using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationService _service;

    public QuotationsController(IQuotationService service) => _service = service;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? CurrentRole => User.FindFirstValue(ClaimTypes.Role);
    private bool IsStaff => CurrentRole is "Sales" or "Manager" or "Admin";

    [HttpGet]
    public async Task<ActionResult<IList<QuotationDto>>> GetList()
    {
        if (IsStaff) return Ok(await _service.GetAllAsync());
        return Ok(await _service.GetMineAsync(CurrentUserId));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IList<QuotationDto>>> GetMine()
        => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpPost]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<ActionResult<QuotationDto>> Create([FromBody] CreateQuotationDto dto)
    {
        try { return Ok(await _service.CreateAsync(CurrentUserId, dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<ActionResult<QuotationDto>> Update(int id, [FromBody] UpdateQuotationDto dto)
    {
        try { return Ok(await _service.UpdateAsync(id, CurrentUserId, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
