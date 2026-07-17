using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IList<CartItemDto>>> Get()
        => Ok(await _cart.GetAsync(CurrentUserId));

    [HttpPost]
    public async Task<ActionResult<CartItemDto>> Add([FromBody] AddCartItemDto dto)
    {
        try { return Ok(await _cart.AddAsync(CurrentUserId, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CartItemDto>> Update(int id, [FromBody] UpdateCartItemDto dto)
    {
        try { return Ok(await _cart.UpdateAsync(CurrentUserId, id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        try
        {
            await _cart.RemoveAsync(CurrentUserId, id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
