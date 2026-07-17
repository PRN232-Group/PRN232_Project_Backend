using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? CurrentRole => User.FindFirstValue(ClaimTypes.Role);

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutRequestDto dto)
    {
        try { return Ok(await _orders.CheckoutAsync(CurrentUserId, dto)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet]
    public async Task<ActionResult<IList<OrderDto>>> GetList()
    {
        var role = CurrentRole;
        if (role is "Sales" or "Manager" or "Admin")
            return Ok(await _orders.GetAllAsync());
        return Ok(await _orders.GetMineAsync(CurrentUserId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        try
        {
            var order = await _orders.GetByIdAsync(id, CurrentUserId, CurrentRole);
            return order == null ? NotFound() : Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Sales,Admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        try { return Ok(await _orders.UpdateStatusAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
