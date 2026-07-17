namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class CategoryUpsertDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class ProductSpecsDto
{
    public string? Dimensions { get; set; }
    public string? Material { get; set; }
    public string? Origin { get; set; }
    public string? Finish { get; set; }
    public decimal? WeightKg { get; set; }
    public int? WarrantyMonths { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? MarketPrice { get; set; }
    public int Stock { get; set; }
    /** Khách chỉ dùng flag này — không hiển thị số tồn */
    public bool InStock { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public ProductSpecsDto? Specs { get; set; }
}

public class ProductUpsertDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? MarketPrice { get; set; }
    public int Stock { get; set; }
    public int? CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public ProductSpecsDto? Specs { get; set; }
}

public class UpdatePriceDto
{
    public decimal Price { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool InStock { get; set; }
}

public class AddCartItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

public class CheckoutCustomerInfoDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public string? PaymentMethod { get; set; }
}

public class CheckoutItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? Price { get; set; }
}

public class CheckoutRequestDto
{
    public string ShippingAddress { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Note { get; set; }
    public CheckoutCustomerInfoDto? CustomerInfo { get; set; }
    public List<CheckoutItemDto>? Items { get; set; }
    public decimal? TotalPrice { get; set; }
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? FullName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string ShippingAddress { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = null!;
}
