using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly IReviewService _reviews;

    public ProductsController(IProductService products, IReviewService reviews)
    {
        _products = products;
        _reviews = reviews;
    }

    private int? CurrentUserId
    {
        get
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IList<ProductDto>>> GetAll()
        => Ok(await _products.GetAllAsync());

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IList<ProductDto>>> Search([FromQuery] string? keyword)
        => Ok(await _products.SearchAsync(keyword));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var p = await _products.GetByIdAsync(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductUpsertDto dto)
        => Ok(await _products.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] ProductUpsertDto dto)
    {
        try { return Ok(await _products.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPut("{id:int}/price")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ProductDto>> UpdatePrice(int id, [FromBody] UpdatePriceDto dto)
    {
        try { return Ok(await _products.UpdatePriceAsync(id, dto.Price)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _products.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("{id:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<IList<ReviewDto>>> GetReviews(int id)
        => Ok(await _reviews.GetByProductAsync(id));

    [HttpPost("{id:int}/reviews")]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> CreateReview(int id, [FromBody] CreateReviewDto dto)
    {
        if (CurrentUserId == null) return Unauthorized();
        try { return Ok(await _reviews.CreateAsync(id, CurrentUserId.Value, dto)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
