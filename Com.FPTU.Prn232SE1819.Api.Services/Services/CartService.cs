using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<CartItemDto>> GetAsync(int userId)
    {
        var cart = await GetOrCreateCart(userId);
        var items = await _uow.Repository<CartItem>().Entities
            .Include(i => i.Product)
            .Where(i => i.CartId == cart.Id)
            .ToListAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<CartItemDto> AddAsync(int userId, AddCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

        var cart = await GetOrCreateCart(userId);

        await _uow.BeginTransactionAsync();
        try
        {
            var product = await _uow.Repository<Product>().Entities
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted && p.IsActive)
                ?? throw new KeyNotFoundException($"Product {dto.ProductId} not found.");

            var existing = await _uow.Repository<CartItem>().Entities
                .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == dto.ProductId);

            var desiredInMyCart = (existing?.Quantity ?? 0) + dto.Quantity;
            await EnsureCartStockAsync(product, cart.Id, desiredInMyCart);

            if (existing != null)
            {
                existing.Quantity = desiredInMyCart;
                await _uow.Repository<CartItem>().UpdateAsync(existing, saveChanges: false);
                await _uow.CommitTransactionAsync();
                existing.Product = product;
                return ToDto(existing);
            }

            var item = new CartItem
            {
                CartId = cart.Id,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
            };
            await _uow.Repository<CartItem>().InsertAsync(item, saveChanges: false);
            cart.UpdatedAt = VnDateTime.Now;
            await _uow.Repository<Cart>().UpdateAsync(cart, saveChanges: false);
            await _uow.CommitTransactionAsync();
            item.Product = product;
            return ToDto(item);
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<CartItemDto> UpdateAsync(int userId, int cartItemId, UpdateCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

        var cart = await GetOrCreateCart(userId);

        await _uow.BeginTransactionAsync();
        try
        {
            var item = await _uow.Repository<CartItem>().Entities
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.CartId == cart.Id)
                ?? throw new KeyNotFoundException($"Cart item {cartItemId} not found.");

            var product = item.Product
                ?? await _uow.Repository<Product>().FindAsync(item.ProductId)
                ?? throw new InvalidOperationException("Sản phẩm không còn khả dụng.");

            if (product.IsDeleted || !product.IsActive)
                throw new InvalidOperationException($"«{product.Name}» không còn bán.");

            await EnsureCartStockAsync(product, cart.Id, dto.Quantity);

            item.Quantity = dto.Quantity;
            await _uow.Repository<CartItem>().UpdateAsync(item, saveChanges: false);
            await _uow.CommitTransactionAsync();
            item.Product = product;
            return ToDto(item);
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RemoveAsync(int userId, int cartItemId)
    {
        var cart = await GetOrCreateCart(userId);
        var item = await _uow.Repository<CartItem>().Entities
            .FirstOrDefaultAsync(i => i.Id == cartItemId && i.CartId == cart.Id)
            ?? throw new KeyNotFoundException($"Cart item {cartItemId} not found.");

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<CartItem>().DeleteAsync(item, saveChanges: false);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Kho còn lại sau khi trừ các giỏ khác (soft-reserve). Ai add muộn hơn khi hết slot sẽ bị từ chối.
    /// </summary>
    private async Task EnsureCartStockAsync(Product product, int myCartId, int desiredInMyCart)
    {
        if (product.Stock <= 0)
            throw new InvalidOperationException($"«{product.Name}» đã hết hàng.");

        var reservedByOthers = await _uow.Repository<CartItem>().Entities
            .Where(i => i.ProductId == product.Id && i.CartId != myCartId)
            .SumAsync(i => (int?)i.Quantity) ?? 0;

        var availableForMe = product.Stock - reservedByOthers;
        if (availableForMe <= 0)
            throw new InvalidOperationException(
                $"«{product.Name}» đã hết hàng.");

        if (desiredInMyCart > availableForMe)
            throw new InvalidOperationException(
                $"«{product.Name}» chỉ còn đủ cho {availableForMe} sản phẩm trong giỏ.");
    }

    private async Task<Cart> GetOrCreateCart(int userId)
    {
        var cart = await _uow.Repository<Cart>().Entities
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null) return cart;

        cart = new Cart { UserId = userId, UpdatedAt = VnDateTime.Now };
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Cart>().InsertAsync(cart);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return cart;
    }

    private static CartItemDto ToDto(CartItem i) => new()
    {
        Id = i.Id,
        ProductId = i.ProductId,
        ProductName = i.Product?.Name ?? "",
        ImageUrl = i.Product?.ImageUrl,
        Price = i.Product?.Price ?? 0,
        Quantity = i.Quantity,
        InStock = (i.Product?.Stock ?? 0) > 0,
    };
}
