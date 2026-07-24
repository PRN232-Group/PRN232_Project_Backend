using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class OrderServiceTests
{
    [Fact]
    public async Task Checkout_with_items_decrements_stock_and_creates_order()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new OrderService(uow, audit.Object);

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "0901234567",
                Items =
                [
                    new CheckoutItemDto { ProductId = 1, Quantity = 2 }
                ],
            });

            Assert.Equal("Pending", order.Status);
            Assert.Equal(20_000_000, order.TotalPrice);
            Assert.Single(order.Items);

            var product = await uow.Repository<Product>().FindAsync(1);
            Assert.Equal(3, product!.Stock);
        }
    }

    [Fact]
    public async Task Checkout_throws_when_stock_insufficient()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CheckoutAsync(1, new CheckoutRequestDto
                {
                    ShippingAddress = "HCM",
                    Phone = "090",
                    Items =
                    [
                        new CheckoutItemDto { ProductId = 1, Quantity = 99 }
                    ],
                }));
        }
    }

    [Fact]
    public async Task GetById_customer_cannot_see_others_order()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            // Extra customer (seed already uses Ids 1–4)
            db.Users.Add(new User
            {
                Id = 10,
                Email = "u2@test.com",
                PasswordHash = "x",
                FullName = "U2",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var svc = new OrderService(uow, Mock.Of<IAuditService>());
            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "A",
                Phone = "1",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.GetByIdAsync(order.Id, requesterId: 10, requesterRole: "Customer"));
        }
    }

    // ── GetMine / GetAll ───────────────────────────────────────────────

    [Fact]
    public async Task GetMine_empty_when_no_orders()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            Assert.Empty(await svc.GetMineAsync(1));
        }
    }

    [Fact]
    public async Task GetMine_returns_only_own_orders()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "A",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });
            await svc.CheckoutAsync(2, new CheckoutRequestDto
            {
                ShippingAddress = "B",
                Phone = "091",
                Items = [new CheckoutItemDto { ProductId = 2, Quantity = 1 }],
            });

            var mine = await svc.GetMineAsync(1);
            Assert.Single(mine);
            Assert.All(mine, o => Assert.Equal(1, o.CustomerId));
        }
    }

    [Fact]
    public async Task GetAll_empty_when_no_orders()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            Assert.Empty(await svc.GetAllAsync());
        }
    }

    [Fact]
    public async Task GetAll_returns_all_orders()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "A",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });
            await svc.CheckoutAsync(2, new CheckoutRequestDto
            {
                ShippingAddress = "B",
                Phone = "091",
                Items = [new CheckoutItemDto { ProductId = 2, Quantity = 1 }],
            });

            var all = await svc.GetAllAsync();
            Assert.Equal(2, all.Count);
        }
    }

    // ── GetById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_returns_null_when_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            Assert.Null(await svc.GetByIdAsync(99999, requesterId: 1, requesterRole: "Customer"));
        }
    }

    [Fact]
    public async Task GetById_staff_can_read_any_order()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "A",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            var asSales = await svc.GetByIdAsync(order.Id, requesterId: 2, requesterRole: "Sales");
            Assert.NotNull(asSales);
            Assert.Equal(order.Id, asSales!.Id);

            var asManager = await svc.GetByIdAsync(order.Id, requesterId: 3, requesterRole: "Manager");
            Assert.NotNull(asManager);

            var asAdmin = await svc.GetByIdAsync(order.Id, requesterId: 4, requesterRole: "Admin");
            Assert.NotNull(asAdmin);
        }
    }

    [Fact]
    public async Task GetById_owner_can_read()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "A",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            var got = await svc.GetByIdAsync(order.Id, requesterId: 1, requesterRole: "Customer");
            Assert.NotNull(got);
            Assert.Equal(order.Id, got!.Id);
            Assert.Equal(1, got.CustomerId);
        }
    }

    // ── Checkout ───────────────────────────────────────────────────────

    [Fact]
    public async Task Checkout_from_cart_clears_cart()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var cartSvc = new CartService(uow);
            var orderSvc = new OrderService(uow, Mock.Of<IAuditService>());

            await cartSvc.AddAsync(1, new AddCartItemDto { ProductId = 1, Quantity = 2 });
            Assert.Single(await cartSvc.GetAsync(1));

            var order = await orderSvc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "0901234567",
            });

            Assert.Equal("Pending", order.Status);
            Assert.Equal(20_000_000, order.TotalPrice);
            Assert.Empty(await cartSvc.GetAsync(1));

            var product = await db.Products.FirstAsync(p => p.Id == 1);
            Assert.Equal(3, product.Stock);
        }
    }

    [Fact]
    public async Task Checkout_empty_cart_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CheckoutAsync(1, new CheckoutRequestDto
                {
                    ShippingAddress = "HCM",
                    Phone = "090",
                }));

            Assert.Contains("trống", ex.Message);
        }
    }

    [Fact]
    public async Task Checkout_missing_shipping_or_phone_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CheckoutAsync(1, new CheckoutRequestDto
                {
                    ShippingAddress = "",
                    Phone = "090",
                    Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
                }));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CheckoutAsync(1, new CheckoutRequestDto
                {
                    ShippingAddress = "HCM",
                    Phone = "  ",
                    Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
                }));
        }
    }

    [Fact]
    public async Task Checkout_with_custom_price_uses_override()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "090",
                Items =
                [
                    new CheckoutItemDto { ProductId = 1, Quantity = 2, Price = 1_000_000 }
                ],
            });

            Assert.Equal(2_000_000, order.TotalPrice);
            Assert.Equal(1_000_000, order.Items[0].Price);
        }
    }

    [Fact]
    public async Task Checkout_inactive_product_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var product = await db.Products.FirstAsync(p => p.Id == 1);
            product.IsActive = false;
            await db.SaveChangesAsync();

            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CheckoutAsync(1, new CheckoutRequestDto
                {
                    ShippingAddress = "HCM",
                    Phone = "090",
                    Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
                }));

            Assert.Contains("not found", ex.Message);
        }
    }

    // ── UpdateStatus ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_Pending_to_Processing_to_Shipping_to_Completed()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new OrderService(uow, audit.Object);

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            var r1 = await svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Processing" }, 2);
            Assert.Equal("Processing", r1.Status);

            var r2 = await svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Shipping" }, 2);
            Assert.Equal("Shipping", r2.Status);

            var r3 = await svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Completed" }, 2);
            Assert.Equal("Completed", r3.Status);

            audit.Verify(a => a.LogAsync(
                "UPDATE_ORDER_STATUS",
                "Order",
                order.Id.ToString(),
                It.IsAny<string?>(),
                2), Times.Exactly(3));
        }
    }

    [Fact]
    public async Task UpdateStatus_Cancel_from_Pending()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new OrderService(uow, audit.Object);

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            var cancelled = await svc.UpdateStatusAsync(
                order.Id,
                new UpdateOrderStatusDto { Status = "Cancelled" },
                actorUserId: 2);

            Assert.Equal("Cancelled", cancelled.Status);
            audit.Verify(a => a.LogAsync(
                "UPDATE_ORDER_STATUS",
                "Order",
                order.Id.ToString(),
                It.IsAny<string?>(),
                2), Times.Once);
        }
    }

    [Fact]
    public async Task UpdateStatus_block_from_Completed()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            await svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Processing" }, 2);
            await svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Shipping" }, 2);
            await svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Completed" }, 2);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Cancelled" }, 2));

            Assert.Contains("đã chốt", ex.Message);
        }
    }

    [Fact]
    public async Task UpdateStatus_invalid_jump_Pending_to_Shipping_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = "Shipping" }, 2));

            Assert.Contains("Pending", ex.Message);
            Assert.Contains("Shipping", ex.Message);
        }
    }

    [Fact]
    public async Task UpdateStatus_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new OrderService(uow, Mock.Of<IAuditService>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateStatusAsync(99999, new UpdateOrderStatusDto { Status = "Processing" }, 2));
        }
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_returns_true_and_audits()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new OrderService(uow, audit.Object);

            var order = await svc.CheckoutAsync(1, new CheckoutRequestDto
            {
                ShippingAddress = "HCM",
                Phone = "090",
                Items = [new CheckoutItemDto { ProductId = 1, Quantity = 1 }],
            });

            var ok = await svc.UpdateOrderStatusAsync(order.Id, "Processing");
            Assert.True(ok);

            var refreshed = await svc.GetByIdAsync(order.Id, 1, "Customer");
            Assert.Equal("Processing", refreshed!.Status);

            audit.Verify(a => a.LogAsync(
                "UPDATE_ORDER_STATUS",
                "Order",
                order.Id.ToString(),
                It.IsAny<string?>(),
                null), Times.Once);
        }
    }
}
