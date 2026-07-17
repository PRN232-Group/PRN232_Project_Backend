namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class QuotationRequestLineDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? MarketPrice { get; set; }
    public int Stock { get; set; }
    public int Quantity { get; set; } = 1;
    public string? ImageUrl { get; set; }
    public ProductSpecsDto? Specs { get; set; }
}

public class QuotationRequestDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public string Status { get; set; } = null!;
    public string? Reply { get; set; }
    public string? ReplyNote { get; set; }
    public DateTime CreatedAt { get; set; }
    /** Alias cho FE cũ — map từ Lines */
    public List<ProductDto> Products { get; set; } = new();
    public List<QuotationRequestLineDto> Lines { get; set; } = new();
    public decimal EstimateTotal { get; set; }
    public decimal MarketTotal { get; set; }
}

public class CreateQuotationRequestItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CreateQuotationRequestDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public List<CreateQuotationRequestItemDto> Items { get; set; } = new();
}

public class ReplyQuotationRequestDto
{
    public string? Reply { get; set; }
    public string? ReplyNote { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

public class UpdateQuotationRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Reply { get; set; }
    public string? ReplyNote { get; set; }
}

public class QuotationLineDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? MarketPrice { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public ProductSpecsDto? Specs { get; set; }
}

public class QuotationDto
{
    public int Id { get; set; }
    public int? QuotationRequestId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string? Title { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalPrice { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QuotationLineDto> Items { get; set; } = new();
    public decimal CatalogTotal { get; set; }
    public decimal MarketTotal { get; set; }
    public decimal Savings { get; set; }
}

public class CreateQuotationDto
{
    public int? QuotationRequestId { get; set; }
    public int? CustomerId { get; set; }
    public string? Title { get; set; }
    public decimal Amount { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public List<CreateQuotationRequestItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
    public string? Note { get; set; }
    public string? Status { get; set; }
}

public class UpdateQuotationDto
{
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? Note { get; set; }
    public decimal? Amount { get; set; }
    public string? Title { get; set; }
}
