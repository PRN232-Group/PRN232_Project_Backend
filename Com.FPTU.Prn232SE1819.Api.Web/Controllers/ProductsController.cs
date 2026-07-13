using Com.FPTU.Prn232SE1819.Api.Caching;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;
using Microsoft.AspNetCore.Mvc;
namespace Com.FPTU.Prn232SE1819.Api.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController: ControllerBase
{
    private readonly IProductService _productService; //IoC
    private readonly IDataCached _dataCached;
    // DI
    public ProductsController(IProductService productService, IDataCached dataCached)
    {
        _productService = productService;
        _dataCached = dataCached;
    }
    /*1. method get return a list products*/
    //GET: api/products
    [HttpGet]
    [ProducesResponseType(200, Type = typeof(IEnumerable<Product>))]
    public async Task<IEnumerable<Product>> GetProducts()
    {

        return await _productService.GetAllAsync();

    }


}


