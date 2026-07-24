using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class CartServiceTests
{
    [Fact]
    public async Task Get_empty_cart_returns_empty_list()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            var items = await svc.GetAsync(1);

            Assert.Empty(items);
        }
    }

    [Fact]
    public async Task Add_new_item_succeeds()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            var item = await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 2 });

            Assert.Equal(1, item.ProductId);
            Assert.Equal(2, item.Quantity);
            Assert.Equal("Sofa Japandi", item.ProductName);
            Assert.Equal(10_000_000, item.Price);
            Assert.True(item.InStock);

            var got = await svc.GetAsync(1);
            Assert.Single(got);
            Assert.Equal(2, got[0].Quantity);
        }
    }

    [Fact]
    public async Task Add_same_product_merges_quantity()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 2 });
            var merged = await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 });

            Assert.Equal(3, merged.Quantity);
            var got = await svc.GetAsync(1);
            Assert.Single(got);
            Assert.Equal(3, got[0].Quantity);
        }
    }

    [Fact]
    public async Task Add_quantity_zero_or_negative_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            var ex0 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 0 }));
            Assert.Contains("lớn hơn 0", ex0.Message);

            var exNeg = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = -1 }));
            Assert.Contains("lớn hơn 0", exNeg.Message);
        }
    }

    [Fact]
    public async Task Add_product_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.AddAsync(1, new AddCartItemDto { ProductId = 999, Quantity = 1 }));
        }
    }

    [Fact]
    public async Task Add_stock_zero_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var product = await db.Products.FirstAsync(p => p.Id == 1);
            product.Stock = 0;
            await db.SaveChangesAsync();

            var svc = new CartService(uow);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 }));

            Assert.Contains("hết hàng", ex.Message);
        }
    }

    [Fact]
    public async Task Add_reserved_by_other_cart_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            // User 2 (Sales) reserves all stock of product 1 (stock=5)
            await svc.AddAsync(2, new AddCartItemDto { ProductId = 1, Quantity = 5 });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 }));

            Assert.Contains("hết hàng", ex.Message);
        }
    }

    [Fact]
    public async Task Update_success_changes_quantity()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            var added = await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 });
            var updated = await svc.UpdateAsync(1, added.Id, new UpdateCartItemDto { Quantity = 4 });

            Assert.Equal(4, updated.Quantity);
            var got = await svc.GetAsync(1);
            Assert.Equal(4, got[0].Quantity);
        }
    }

    [Fact]
    public async Task Update_quantity_zero_or_negative_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            var added = await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateAsync(1, added.Id, new UpdateCartItemDto { Quantity = 0 }));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateAsync(1, added.Id, new UpdateCartItemDto { Quantity = -2 }));
        }
    }

    [Fact]
    public async Task Update_wrong_item_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 });

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(1, cartItemId: 99999, new UpdateCartItemDto { Quantity = 2 }));
        }
    }

    [Fact]
    public async Task Remove_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            var added = await svc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 1 });
            await svc.RemoveAsync(1, added.Id);

            Assert.Empty(await svc.GetAsync(1));
            Assert.False(await db.CartItems.AnyAsync(i => i.Id == added.Id));
        }
    }

    [Fact]
    public async Task Remove_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CartService(uow);

            // Ensure cart exists
            await svc.GetAsync(1);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.RemoveAsync(1, cartItemId: 99999));
        }
    }
}
