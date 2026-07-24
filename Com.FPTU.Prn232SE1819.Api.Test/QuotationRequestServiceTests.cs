using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class QuotationRequestServiceTests
{
    [Fact]
    public async Task GetAll_and_GetMine_empty()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            Assert.Empty(await svc.GetAllAsync());
            Assert.Empty(await svc.GetMineAsync(1));
        }
    }

    [Fact]
    public async Task GetAll_and_GetMine_filtered_by_customer()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Users.Add(new User
            {
                Id = 5,
                Email = "kh2@test.com",
                PasswordHash = "x",
                FullName = "Khach 2",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var svc = new QuotationRequestService(uow);
            await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "Mine",
                ProductIds = [1],
            });
            await svc.CreateAsync(5, new CreateQuotationRequestDto
            {
                Title = "Other",
                ProductIds = [2],
            });

            var all = await svc.GetAllAsync();
            Assert.Equal(2, all.Count);

            var mine = await svc.GetMineAsync(1);
            Assert.Single(mine);
            Assert.Equal("Mine", mine[0].Title);
            Assert.Empty(await svc.GetMineAsync(3));
        }
    }

    [Fact]
    public async Task Create_from_Items_sets_quantities()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            var created = await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "Items",
                Description = "desc",
                Items =
                [
                    new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 3 },
                    new CreateQuotationRequestItemDto { ProductId = 2, Quantity = 2 },
                ],
            });

            Assert.Equal("Pending", created.Status);
            Assert.Equal(2, created.Lines.Count);
            Assert.Equal(3, created.Lines.First(l => l.ProductId == 1).Quantity);
            Assert.Equal(2, created.Lines.First(l => l.ProductId == 2).Quantity);
            Assert.Equal(1, created.CustomerId);
        }
    }

    [Fact]
    public async Task Create_from_ProductIds_defaults_qty_1()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            var created = await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "ByIds",
                ProductIds = [1, 2],
            });

            Assert.Equal(2, created.ProductIds.Count);
            Assert.All(created.Lines, l => Assert.Equal(1, l.Quantity));
        }
    }

    [Fact]
    public async Task Create_merges_duplicate_Items_and_adds_missing_ProductIds()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            var created = await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "Merge",
                Items =
                [
                    new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 2 },
                    new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 3 },
                ],
                ProductIds = [1, 2],
            });

            Assert.Equal(2, created.Lines.Count);
            Assert.Equal(5, created.Lines.First(l => l.ProductId == 1).Quantity);
            Assert.Equal(1, created.Lines.First(l => l.ProductId == 2).Quantity);
        }
    }

    [Fact]
    public async Task Create_qty_lte_0_becomes_1()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            var created = await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "Qty",
                Items =
                [
                    new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 0 },
                    new CreateQuotationRequestItemDto { ProductId = 2, Quantity = -5 },
                ],
            });

            Assert.All(created.Lines, l => Assert.Equal(1, l.Quantity));
        }
    }

    [Fact]
    public async Task Create_blank_title_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, new CreateQuotationRequestDto
                {
                    Title = "   ",
                    ProductIds = [1],
                }));
        }
    }

    [Fact]
    public async Task Create_no_products_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, new CreateQuotationRequestDto
                {
                    Title = "Empty",
                    ProductIds = [0, -1],
                    Items = [new CreateQuotationRequestItemDto { ProductId = 0, Quantity = 1 }],
                }));
        }
    }

    [Fact]
    public async Task Create_inactive_product_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var p = await db.Products.FindAsync(1);
            p!.IsActive = false;
            await db.SaveChangesAsync();

            var svc = new QuotationRequestService(uow);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, new CreateQuotationRequestDto
                {
                    Title = "Inactive",
                    ProductIds = [1],
                }));
        }
    }

    [Fact]
    public async Task Create_deleted_product_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var p = await db.Products.FindAsync(2);
            p!.IsDeleted = true;
            await db.SaveChangesAsync();

            var svc = new QuotationRequestService(uow);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, new CreateQuotationRequestDto
                {
                    Title = "Deleted",
                    ProductIds = [2],
                }));
        }
    }

    [Fact]
    public async Task Create_user_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.CreateAsync(999, new CreateQuotationRequestDto
                {
                    Title = "NoUser",
                    ProductIds = [1],
                }));
        }
    }

    [Fact]
    public async Task ReplyAsync_defaults_to_Replied()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);
            var created = await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "R",
                ProductIds = [1],
            });

            var replied = await svc.ReplyAsync(created.Id, handledById: 2, new ReplyQuotationRequestDto
            {
                ReplyNote = "ok",
            });

            Assert.Equal("Replied", replied.Status);
            Assert.Equal("ok", replied.Reply);
            Assert.Equal("ok", replied.ReplyNote);
        }
    }

    [Fact]
    public async Task ReplyAsync_custom_status_and_Reply_Message_fallback()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);
            var a = await svc.CreateAsync(1, new CreateQuotationRequestDto { Title = "A", ProductIds = [1] });
            var b = await svc.CreateAsync(1, new CreateQuotationRequestDto { Title = "B", ProductIds = [1] });

            var withReply = await svc.ReplyAsync(a.Id, 2, new ReplyQuotationRequestDto
            {
                Reply = "from-reply",
                Status = "Closed",
            });
            Assert.Equal("Closed", withReply.Status);
            Assert.Equal("from-reply", withReply.Reply);

            var withMessage = await svc.ReplyAsync(b.Id, 2, new ReplyQuotationRequestDto
            {
                Message = "from-message",
            });
            Assert.Equal("from-message", withMessage.Reply);
        }
    }

    [Fact]
    public async Task ReplyAsync_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.ReplyAsync(999, 2, new ReplyQuotationRequestDto { Reply = "x" }));
        }
    }

    [Fact]
    public async Task UpdateAsync_partial_fields()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);
            var created = await svc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "Old",
                Description = "d1",
                ProductIds = [1],
            });

            var updated = await svc.UpdateAsync(created.Id, new UpdateQuotationRequestDto
            {
                Title = "NewTitle",
                Description = "d2",
                Status = "InProgress",
                ReplyNote = "note",
            });

            Assert.Equal("NewTitle", updated.Title);
            Assert.Equal("d2", updated.Description);
            Assert.Equal("InProgress", updated.Status);
            Assert.Equal("note", updated.Reply);

            var titleOnly = await svc.UpdateAsync(created.Id, new UpdateQuotationRequestDto
            {
                Title = "   ",
                Reply = "via-reply",
            });
            Assert.Equal("NewTitle", titleOnly.Title);
            Assert.Equal("via-reply", titleOnly.Reply);
        }
    }

    [Fact]
    public async Task UpdateAsync_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new QuotationRequestService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(999, new UpdateQuotationRequestDto { Title = "x" }));
        }
    }
}
