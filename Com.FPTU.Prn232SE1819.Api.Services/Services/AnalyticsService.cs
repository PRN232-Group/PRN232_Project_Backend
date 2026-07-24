using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _uow;
    private readonly IQuotationRequestService _quotationRequests;
    private readonly IQuotationService _quotations;

    public AnalyticsService(
        IUnitOfWork uow,
        IQuotationRequestService quotationRequests,
        IQuotationService quotations)
    {
        _uow = uow;
        _quotationRequests = quotationRequests;
        _quotations = quotations;
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync()
    {
        var activeOrders = _uow.Repository<Order>().Entities.Where(o => o.Status != "Cancelled");

        var totalRevenue = await activeOrders.SumAsync(o => o.TotalPrice);
        var totalOrders = await activeOrders.CountAsync();
        var totalProducts = await _uow.Repository<Product>().Entities
            .CountAsync(p => !p.IsDeleted);
        var totalUsers = await _uow.Repository<User>().Entities
            .CountAsync(u => !u.IsDeleted);
        var totalCustomers = await _uow.Repository<User>().Entities
            .CountAsync(u => !u.IsDeleted && u.Role.Name == "Customer");

        var bestSelling = (await GetBestSellingProductsAsync()).FirstOrDefault();

        return new AnalyticsDashboardDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            TotalUsers = totalUsers,
            BestSellingProduct = bestSelling,
        };
    }

    public async Task<IList<BestSellingProductDto>> GetBestSellingProductsAsync()
    {
        var rows = await (
            from oi in _uow.Repository<OrderItem>().Entities
            join o in _uow.Repository<Order>().Entities on oi.OrderId equals o.Id
            where o.Status != "Cancelled"
            group oi by new { oi.ProductId, oi.ProductName } into g
            orderby g.Sum(x => x.Quantity) descending
            select new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                SoldQuantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
            }).ToListAsync();

        return rows.Select(r => new BestSellingProductDto
        {
            Id = r.ProductId,
            ProductId = r.ProductId,
            Name = r.ProductName,
            Sold = r.SoldQuantity,
            SoldQuantity = r.SoldQuantity,
            Revenue = r.Revenue,
        }).ToList();
    }

    public async Task<IList<RevenueReportItemDto>> GetRevenueReportAsync(DateTime? from, DateTime? to)
    {
        var query = _uow.Repository<Order>().Entities
            .Where(o => o.Status != "Cancelled");

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value.Date);

        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            query = query.Where(o => o.CreatedAt < end);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new RevenueReportItemDto
            {
                Id = o.Id,
                Date = o.CreatedAt,
                OrderCode = $"ORD-{o.Id:D6}",
                Amount = o.TotalPrice,
            })
            .ToListAsync();
    }

    public async Task<SalesDashboardDto> GetSalesDashboardAsync()
    {
        var orders = _uow.Repository<Order>().Entities;
        var quotationRequests = _uow.Repository<QuotationRequest>().Entities;
        var quotationList = _uow.Repository<Quotation>().Entities;

        var allRequests = await _quotationRequests.GetAllAsync();
        var allQuotations = await _quotations.GetAllAsync();

        return new SalesDashboardDto
        {
            PendingOrders = await orders.CountAsync(o => o.Status == "Pending"),
            Quotations = await quotationList.CountAsync(),
            DesignRequests = await _uow.Repository<DesignRequest>().Entities.CountAsync(),
            Chats = await _uow.Repository<ChatThread>().Entities.CountAsync(),
            Stats = new SalesStatsDto
            {
                ProcessingOrders = await orders.CountAsync(o => o.Status == "Processing"),
                ShippingOrders = await orders.CountAsync(o => o.Status == "Shipping"),
                CompletedOrders = await orders.CountAsync(o => o.Status == "Completed"),
                PendingQuotationRequests = await quotationRequests.CountAsync(r => r.Status == "Pending"),
            },
            RecentRequests = allRequests.Take(5).ToList(),
            RecentQuotations = allQuotations.Take(5).ToList(),
        };
    }
}
