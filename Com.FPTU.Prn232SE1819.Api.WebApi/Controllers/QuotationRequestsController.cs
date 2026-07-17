using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/quotation-requests")]
[Authorize]
public class QuotationRequestsController : ControllerBase
{
    private readonly IQuotationRequestService _service;

    public QuotationRequestsController(IQuotationRequestService service) => _service = service;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? CurrentRole => User.FindFirstValue(ClaimTypes.Role);
    private bool IsStaff => CurrentRole is "Sales" or "Manager" or "Admin";

    [HttpGet]
    public async Task<ActionResult<IList<QuotationRequestDto>>> GetList()
    {
        if (IsStaff) return Ok(await _service.GetAllAsync());
        return Ok(await _service.GetMineAsync(CurrentUserId));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IList<QuotationRequestDto>>> GetMine()
        => Ok(await _service.GetMineAsync(CurrentUserId));

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<QuotationRequestDto>> Create([FromBody] CreateQuotationRequestDto dto)
    {
        try { return Ok(await _service.CreateAsync(CurrentUserId, dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/reply")]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<ActionResult<QuotationRequestDto>> Reply(int id, [FromBody] ReplyQuotationRequestDto dto)
    {
        try { return Ok(await _service.ReplyAsync(id, CurrentUserId, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<ActionResult<QuotationRequestDto>> Update(int id, [FromBody] UpdateQuotationRequestDto dto)
    {
        try { return Ok(await _service.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
