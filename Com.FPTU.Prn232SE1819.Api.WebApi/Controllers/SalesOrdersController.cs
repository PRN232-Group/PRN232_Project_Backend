using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/sales/orders")]
[Authorize(Roles = "Sales,Admin")]
public class SalesOrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public SalesOrdersController(IOrderService orders) => _orders = orders;

    [HttpGet]
    public async Task<ActionResult<IList<OrderDto>>> GetAll()
        => Ok(await _orders.GetAllAsync());

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderDto>> Update(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        try { return Ok(await _orders.UpdateStatusAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
