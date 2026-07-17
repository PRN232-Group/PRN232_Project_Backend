using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardDto> GetDashboardAsync();
    Task<IList<BestSellingProductDto>> GetBestSellingProductsAsync();
    Task<IList<RevenueReportItemDto>> GetRevenueReportAsync(DateTime? from, DateTime? to);
    Task<SalesDashboardDto> GetSalesDashboardAsync();
}
