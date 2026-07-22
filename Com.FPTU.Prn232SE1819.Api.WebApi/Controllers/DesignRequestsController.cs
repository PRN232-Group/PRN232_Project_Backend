using System.Security.Claims;
using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/design-requests")]
[Authorize]
public class DesignRequestsController : ControllerBase
{
    private readonly IDesignRequestService _service;

    public DesignRequestsController(IDesignRequestService service)
    {
        _service = service;
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id") ?? "0");

    private string GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreateDesignRequestDto dto)
    {
        var result = await _service.CreateAsync(GetCurrentUserId(), dto);
        return StatusCode(201, result);
    }

    [HttpGet]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMine()
    {
        var list = await _service.GetMineAsync(GetCurrentUserId());
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var req = await _service.GetByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            if (req == null) return NotFound(new { message = "Không tìm thấy yêu cầu thiết kế." });
            return Ok(req);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDesignRequestStatusDto dto)
    {
        try
        {
            var result = await _service.UpdateStatusAsync(id, dto.Status);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}