using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("/api/analytics/dashboard")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<AnalyticsDashboardDto>> Dashboard()
        => Ok(await _analytics.GetDashboardAsync());

    [HttpGet("/api/analytics/best-selling-products")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IList<BestSellingProductDto>>> BestSellingProducts()
        => Ok(await _analytics.GetBestSellingProductsAsync());

    [HttpGet("/api/revenue/report")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IList<RevenueReportItemDto>>> RevenueReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Ok(await _analytics.GetRevenueReportAsync(from, to));

    [HttpGet("/api/sales/dashboard")]
    [Authorize(Roles = "Sales,Manager,Admin")]
    public async Task<ActionResult<SalesDashboardDto>> SalesDashboard()
        => Ok(await _analytics.GetSalesDashboardAsync());
}
