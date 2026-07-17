namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class AnalyticsDashboardDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalUsers { get; set; }
    public BestSellingProductDto? BestSellingProduct { get; set; }
}

public class BestSellingProductDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Sold { get; set; }
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
}

public class RevenueReportItemDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string OrderCode { get; set; } = "";
    public decimal Amount { get; set; }
}

public class SalesStatsDto
{
    public int ProcessingOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingQuotationRequests { get; set; }
}

public class SalesDashboardDto
{
    public int PendingOrders { get; set; }
    public int Quotations { get; set; }
    public int DesignRequests { get; set; }
    public int Chats { get; set; }
    public SalesStatsDto Stats { get; set; } = new();
    public List<QuotationRequestDto> RecentRequests { get; set; } = new();
    public List<QuotationDto> RecentQuotations { get; set; } = new();
}
