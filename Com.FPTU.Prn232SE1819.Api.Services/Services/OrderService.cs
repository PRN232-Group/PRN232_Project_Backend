using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Com.FPTU.Prn232SE1819.Api.Services.Common;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;

    public OrderService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<OrderDto>> GetMineAsync(int userId)
    {
        var list = await OrderQuery()
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<IList<OrderDto>> GetAllAsync()
    {
        var list = await OrderQuery()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(int id, int? requesterId, string? requesterRole)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return null;

        var isStaff = IsStaff(requesterRole);
        if (!isStaff && requesterId != order.CustomerId)
            throw new UnauthorizedAccessException("Not your order.");

        return ToDto(order);
    }

    public async Task<OrderDto> CheckoutAsync(int userId, CheckoutRequestDto dto)
    {
        var user = await _uow.Repository<User>().FindAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var lines = new List<(Product product, int qty, decimal unitPrice)>();

        if (dto.Items != null && dto.Items.Count > 0)
        {
            foreach (var line in dto.Items)
            {
                var p = await _uow.Repository<Product>().Entities
                    .FirstOrDefaultAsync(x => x.Id == line.ProductId && !x.IsDeleted && x.IsActive)
                    ?? throw new InvalidOperationException($"Product {line.ProductId} not found.");
                if (p.Stock < line.Quantity)
                    throw new InvalidOperationException(
                        $"«{p.Name}» không đủ hàng (còn {p.Stock}). Vui lòng giảm số lượng hoặc chọn sản phẩm khác.");
                lines.Add((p, line.Quantity, line.Price ?? p.Price));
            }
        }
        else
        {
            var cart = await _uow.Repository<Cart>().Entities
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new InvalidOperationException("Giỏ hàng trống.");
            var cartItems = await _uow.Repository<CartItem>().Entities
                .Include(i => i.Product)
                .Where(i => i.CartId == cart.Id)
                .ToListAsync();
            if (cartItems.Count == 0) throw new InvalidOperationException("Giỏ hàng trống.");
            foreach (var ci in cartItems)
            {
                if (ci.Product == null || ci.Product.IsDeleted || !ci.Product.IsActive)
                    throw new InvalidOperationException("Giỏ có sản phẩm không còn bán.");
                if (ci.Product.Stock < ci.Quantity)
                    throw new InvalidOperationException(
                        $"«{ci.Product.Name}» không đủ hàng (còn {ci.Product.Stock}). Vui lòng cập nhật giỏ rồi thử lại.");
                lines.Add((ci.Product, ci.Quantity, ci.Product.Price));
            }
        }

        var shipping = dto.CustomerInfo?.Address ?? dto.ShippingAddress;
        var phone = dto.CustomerInfo?.Phone ?? dto.Phone;
        if (string.IsNullOrWhiteSpace(shipping) || string.IsNullOrWhiteSpace(phone))
            throw new InvalidOperationException("Cần địa chỉ giao hàng và số điện thoại.");

        var total = lines.Sum(l => l.unitPrice * l.qty);
        var order = new Order
        {
            CustomerId = userId,
            Status = "Pending",
            TotalPrice = total,
            ShippingAddress = shipping.Trim(),
            Phone = phone.Trim(),
            CustomerName = dto.CustomerInfo?.FullName ?? user.FullName,
            CustomerEmail = user.Email,
            Note = dto.CustomerInfo?.Note ?? dto.Note,
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            // Re-check stock inside transaction (race giữa 2 checkout cùng lúc)
            foreach (var (product, qty, _) in lines)
            {
                var fresh = await _uow.Repository<Product>().Entities
                    .FirstOrDefaultAsync(p => p.Id == product.Id)
                    ?? throw new InvalidOperationException($"Sản phẩm #{product.Id} không tồn tại.");
                if (fresh.IsDeleted || !fresh.IsActive)
                    throw new InvalidOperationException($"«{fresh.Name}» không còn bán.");
                if (fresh.Stock < qty)
                    throw new InvalidOperationException(
                        $"«{fresh.Name}» vừa hết / không đủ hàng (còn {fresh.Stock}). Ai thanh toán trước giữ chỗ — vui lòng thử lại.");
            }

            await _uow.Repository<Order>().InsertAsync(order, saveChanges: false);
            await _uow.SaveChangesAsync();

            foreach (var (product, qty, unitPrice) in lines)
            {
                var fresh = await _uow.Repository<Product>().Entities
                    .FirstAsync(p => p.Id == product.Id);

                await _uow.Repository<OrderItem>().InsertAsync(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = fresh.Id,
                    ProductName = fresh.Name,
                    UnitPrice = unitPrice,
                    Quantity = qty,
                }, saveChanges: false);

                fresh.Stock -= qty;
                fresh.UpdatedAt = VnDateTime.Now;
                await _uow.Repository<Product>().UpdateAsync(fresh, saveChanges: false);
            }

            var cart = await _uow.Repository<Cart>().Entities
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart != null)
            {
                var cartItems = await _uow.Repository<CartItem>().Entities
                    .Where(i => i.CartId == cart.Id)
                    .ToListAsync();
                foreach (var ci in cartItems)
                    await _uow.Repository<CartItem>().DeleteAsync(ci, saveChanges: false);
            }

            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        return (await GetByIdAsync(order.Id, userId, "Customer"))!;
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        // --- CODE CỦA BẠN ĐƯỢC THÊM VÀO TỪ ĐÂY ---
        var newStatus = dto.Status.Trim();

        // 1. Chặn lại ngay nếu đơn hàng đã hoàn thành hoặc đã hủy
        if (order.Status == "Completed" || order.Status == "Cancelled")
        {
            throw new InvalidOperationException("Đơn hàng đã chốt, không thể đổi trạng thái.");
        }

        // 2. Kiểm tra luồng đi hợp lệ (tịnh tiến dần)
        bool isValid = false;

        if (newStatus == "Cancelled")
        {
            isValid = true; // Cho phép hủy bất cứ lúc nào (vì đã qua vòng chặn bên trên)
        }
        else if (order.Status == "Pending" && newStatus == "Processing") isValid = true;
        else if (order.Status == "Processing" && newStatus == "Shipping") isValid = true;
        else if (order.Status == "Shipping" && newStatus == "Completed") isValid = true;

        if (!isValid)
        {
            throw new InvalidOperationException($"Không thể chuyển trạng thái từ {order.Status} sang {newStatus}.");
        }
        // --- KẾT THÚC PHẦN CODE CỦA BẠN ---

        // --- ĐÂY LÀ CODE GỐC CỦA TRUNG ĐƯỢC GIỮ NGUYÊN ---
        order.Status = newStatus;
        order.UpdatedAt = VnDateTime.Now;
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Order>().UpdateAsync(order);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return ToDto(order);
    }

    private IQueryable<Order> OrderQuery()
        => _uow.Repository<Order>().Entities
            .Include(o => o.OrderItems)
            .Include(o => o.Customer);

    private static bool IsStaff(string? role)
        => role is "Sales" or "Manager" or "Admin";

    private static OrderDto ToDto(Order o)
    {
        var items = o.OrderItems.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Name = i.ProductName,
            Quantity = i.Quantity,
            Price = i.UnitPrice,
        }).ToList();

        return new OrderDto
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            CustomerName = o.CustomerName ?? o.Customer?.FullName,
            FullName = o.CustomerName ?? o.Customer?.FullName,
            CustomerEmail = o.CustomerEmail ?? o.Customer?.Email,
            CustomerPhone = o.Phone,
            ShippingAddress = o.ShippingAddress,
            Phone = o.Phone,
            Address = o.ShippingAddress,
            Status = o.Status,
            TotalPrice = o.TotalPrice,
            CreatedAt = o.CreatedAt,
            Items = items,
        };
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        await UpdateStatusAsync(orderId, new UpdateOrderStatusDto { Status = newStatus });
        return true;
    }
}
