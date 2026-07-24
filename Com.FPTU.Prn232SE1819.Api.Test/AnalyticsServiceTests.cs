using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task Dashboard_sums_non_cancelled_orders()
    {
        var (db, uow, sp) = TestDb.Create();
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);

            db.Orders.AddRange(
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 100,
                    ShippingAddress = "A",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Pending",
                    TotalPrice = 50,
                    ShippingAddress = "B",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Cancelled",
                    TotalPrice = 999,
                    ShippingAddress = "C",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var dash = await svc.GetDashboardAsync();

            Assert.Equal(150, dash.TotalRevenue);
            Assert.Equal(2, dash.TotalOrders);
            Assert.Equal(2, dash.TotalProducts);
            Assert.Equal(4, dash.TotalUsers);
            Assert.Equal(1, dash.TotalCustomers);
        }
    }

    [Fact]
    public async Task BestSelling_aggregates_by_product_excluding_cancelled()
    {
        var (db, uow, sp) = TestDb.Create();
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);

            var ok = new Order
            {
                CustomerId = 1,
                Status = "Completed",
                TotalPrice = 200,
                ShippingAddress = "A",
                Phone = "0900",
                CreatedAt = DateTime.UtcNow,
            };
            var cancelled = new Order
            {
                CustomerId = 1,
                Status = "Cancelled",
                TotalPrice = 500,
                ShippingAddress = "B",
                Phone = "0900",
                CreatedAt = DateTime.UtcNow,
            };
            db.Orders.AddRange(ok, cancelled);
            await db.SaveChangesAsync();

            db.OrderItems.AddRange(
                new OrderItem
                {
                    OrderId = ok.Id,
                    ProductId = 1,
                    ProductName = "Sofa Japandi",
                    Quantity = 2,
                    UnitPrice = 100,
                },
                new OrderItem
                {
                    OrderId = cancelled.Id,
                    ProductId = 1,
                    ProductName = "Sofa Japandi",
                    Quantity = 99,
                    UnitPrice = 100,
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var rows = await svc.GetBestSellingProductsAsync();
            Assert.Single(rows);
            Assert.Equal(2, rows[0].SoldQuantity);
            Assert.Equal(200, rows[0].Revenue);
        }
    }

    [Fact]
    public async Task RevenueReport_filters_by_date_and_excludes_cancelled()
    {
        var (db, uow, sp) = TestDb.Create();
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);

            var day = new DateTime(2026, 7, 20);
            db.Orders.AddRange(
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 10,
                    ShippingAddress = "A",
                    Phone = "0900",
                    CreatedAt = day,
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 20,
                    ShippingAddress = "B",
                    Phone = "0900",
                    CreatedAt = day.AddDays(5),
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Cancelled",
                    TotalPrice = 30,
                    ShippingAddress = "C",
                    Phone = "0900",
                    CreatedAt = day.AddDays(1),
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var report = await svc.GetRevenueReportAsync(day, day.AddDays(2));
            Assert.Single(report);
            Assert.Equal(10, report[0].Amount);
            Assert.StartsWith("ORD-", report[0].OrderCode);
        }
    }

    [Fact]
    public async Task Dashboard_empty_zeros()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var dash = await svc.GetDashboardAsync();

            Assert.Equal(0, dash.TotalRevenue);
            Assert.Equal(0, dash.TotalOrders);
            Assert.Equal(0, dash.TotalProducts);
            Assert.Equal(0, dash.TotalUsers);
            Assert.Equal(0, dash.TotalCustomers);
            Assert.Null(dash.BestSellingProduct);
        }
    }

    [Fact]
    public async Task BestSelling_multi_product_ordered_by_quantity_desc()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);

            var order = new Order
            {
                CustomerId = 1,
                Status = "Completed",
                TotalPrice = 500,
                ShippingAddress = "A",
                Phone = "0900",
                CreatedAt = DateTime.UtcNow,
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            db.OrderItems.AddRange(
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = 1,
                    ProductName = "Sofa Japandi",
                    Quantity = 2,
                    UnitPrice = 100,
                },
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = 2,
                    ProductName = "Bàn trà",
                    Quantity = 5,
                    UnitPrice = 50,
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var rows = await svc.GetBestSellingProductsAsync();
            Assert.Equal(2, rows.Count);
            Assert.Equal(2, rows[0].ProductId);
            Assert.Equal(5, rows[0].SoldQuantity);
            Assert.Equal(1, rows[1].ProductId);
            Assert.Equal(2, rows[1].SoldQuantity);
        }
    }

    [Fact]
    public async Task RevenueReport_from_only()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var day = new DateTime(2026, 7, 10);
            db.Orders.AddRange(
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 1,
                    ShippingAddress = "A",
                    Phone = "0900",
                    CreatedAt = day.AddDays(-2),
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 2,
                    ShippingAddress = "B",
                    Phone = "0900",
                    CreatedAt = day,
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var report = await svc.GetRevenueReportAsync(day, null);
            Assert.Single(report);
            Assert.Equal(2, report[0].Amount);
        }
    }

    [Fact]
    public async Task RevenueReport_to_only()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var day = new DateTime(2026, 7, 10);
            db.Orders.AddRange(
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 1,
                    ShippingAddress = "A",
                    Phone = "0900",
                    CreatedAt = day,
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 2,
                    ShippingAddress = "B",
                    Phone = "0900",
                    CreatedAt = day.AddDays(5),
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var report = await svc.GetRevenueReportAsync(null, day);
            Assert.Single(report);
            Assert.Equal(1, report[0].Amount);
        }
    }

    [Fact]
    public async Task RevenueReport_neither_returns_all_non_cancelled()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Orders.AddRange(
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 1,
                    ShippingAddress = "A",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Cancelled",
                    TotalPrice = 9,
                    ShippingAddress = "B",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(
                uow,
                Mock.Of<IQuotationRequestService>(),
                Mock.Of<IQuotationService>());

            var report = await svc.GetRevenueReportAsync(null, null);
            Assert.Single(report);
            Assert.Equal(1, report[0].Amount);
        }
    }

    [Fact]
    public async Task GetSalesDashboardAsync_counters()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);

            db.Orders.AddRange(
                new Order
                {
                    CustomerId = 1,
                    Status = "Pending",
                    TotalPrice = 10,
                    ShippingAddress = "A",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Processing",
                    TotalPrice = 20,
                    ShippingAddress = "B",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Shipping",
                    TotalPrice = 30,
                    ShippingAddress = "C",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                },
                new Order
                {
                    CustomerId = 1,
                    Status = "Completed",
                    TotalPrice = 40,
                    ShippingAddress = "D",
                    Phone = "0900",
                    CreatedAt = DateTime.UtcNow,
                });

            db.QuotationRequests.Add(new QuotationRequest
            {
                CustomerId = 1,
                Title = "QR",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            });
            db.Quotations.Add(new Quotation
            {
                CustomerId = 1,
                Title = "Q",
                Amount = 100,
                Status = "PendingApproval",
                CreatedAt = DateTime.UtcNow,
            });
            db.DesignRequests.Add(new DesignRequest
            {
                CustomerId = 1,
                Title = "DR",
                Status = "New",
                CreatedAt = DateTime.UtcNow,
            });
            db.ChatThreads.Add(new ChatThread
            {
                CustomerId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var reqList = new List<QuotationRequestDto>
            {
                new() { Id = 1, Title = "R1", Status = "Pending", CustomerName = "A", CreatedAt = DateTime.UtcNow },
                new() { Id = 2, Title = "R2", Status = "Pending", CustomerName = "B", CreatedAt = DateTime.UtcNow },
            };
            var quotList = new List<QuotationDto>
            {
                new() { Id = 1, Title = "Q1", Status = "PendingApproval", CustomerName = "A", CreatedAt = DateTime.UtcNow },
            };

            var reqMock = new Mock<IQuotationRequestService>();
            reqMock.Setup(s => s.GetAllAsync()).ReturnsAsync(reqList);
            var quotMock = new Mock<IQuotationService>();
            quotMock.Setup(s => s.GetAllAsync()).ReturnsAsync(quotList);

            var svc = new AnalyticsService(uow, reqMock.Object, quotMock.Object);
            var dash = await svc.GetSalesDashboardAsync();

            Assert.Equal(1, dash.PendingOrders);
            Assert.Equal(1, dash.Quotations);
            Assert.Equal(1, dash.DesignRequests);
            Assert.Equal(1, dash.Chats);
            Assert.Equal(1, dash.Stats.ProcessingOrders);
            Assert.Equal(1, dash.Stats.ShippingOrders);
            Assert.Equal(1, dash.Stats.CompletedOrders);
            Assert.Equal(1, dash.Stats.PendingQuotationRequests);
            Assert.Equal(2, dash.RecentRequests.Count);
            Assert.Single(dash.RecentQuotations);
        }
    }
}
