using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class QuotationServiceTests
{
    private static QuotationService CreateSvc(
        Application.Interfaces.Repositories.IUnitOfWork uow,
        Mock<IAuditService>? audit = null)
        => new(uow, (audit ?? new Mock<IAuditService>()).Object);

    [Fact]
    public async Task GetAll_and_GetMine()
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

            var svc = CreateSvc(uow);
            Assert.Empty(await svc.GetAllAsync());

            await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                Title = "Q1",
                ProductIds = [1],
            });
            await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 5,
                Title = "Q2",
                ProductIds = [2],
            });

            Assert.Equal(2, (await svc.GetAllAsync()).Count);
            var mine = await svc.GetMineAsync(1);
            Assert.Single(mine);
            Assert.Equal("Q1", mine[0].Title);
        }
    }

    [Fact]
    public async Task Create_from_Items()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                Title = "Items",
                Items =
                [
                    new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 2 },
                    new CreateQuotationRequestItemDto { ProductId = 2, Quantity = 1 },
                ],
            });

            Assert.Equal("PendingApproval", q.Status);
            Assert.Equal(2, q.Items.Count);
            Assert.Equal(2, q.Items.First(i => i.ProductId == 1).Quantity);
            Assert.Equal(10_000_000m * 2 + 3_000_000m, q.Amount);
        }
    }

    [Fact]
    public async Task Create_from_ProductIds()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                ProductIds = [1, 2],
            });

            Assert.Equal(2, q.ProductIds.Count);
            Assert.All(q.Items, i => Assert.Equal(1, i.Quantity));
            Assert.Equal("PendingApproval", q.Status);
        }
    }

    [Fact]
    public async Task Create_from_QuotationRequest_links_and_sets_Replied()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var reqSvc = new QuotationRequestService(uow);
            var req = await reqSvc.CreateAsync(1, new CreateQuotationRequestDto
            {
                Title = "ReqTitle",
                Items = [new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 2 }],
            });

            var audit = new Mock<IAuditService>();
            var svc = CreateSvc(uow, audit);
            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                QuotationRequestId = req.Id,
                Notes = "Bao gia",
            });

            Assert.Equal(req.Id, q.QuotationRequestId);
            Assert.Equal(1, q.CustomerId);
            Assert.Equal("ReqTitle", q.Title);
            Assert.Equal(2, q.Items.Single().Quantity);

            var updatedReq = await reqSvc.GetMineAsync(1);
            Assert.Equal("Replied", updatedReq[0].Status);
            Assert.Equal("Bao gia", updatedReq[0].Reply);
        }
    }

    [Fact]
    public async Task Create_Amount_override()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                ProductIds = [1],
                Amount = 99_000,
            });

            Assert.Equal(99_000, q.Amount);
        }
    }

    [Fact]
    public async Task Create_missing_customer_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(2, new CreateQuotationDto
                {
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
            var svc = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(2, new CreateQuotationDto
                {
                    CustomerId = 1,
                    ProductIds = [],
                    Items = [],
                }));
        }
    }

    [Fact]
    public async Task Create_ProductId_lte_0_skipped()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                Items =
                [
                    new CreateQuotationRequestItemDto { ProductId = 0, Quantity = 5 },
                    new CreateQuotationRequestItemDto { ProductId = -1, Quantity = 2 },
                    new CreateQuotationRequestItemDto { ProductId = 1, Quantity = 1 },
                ],
                ProductIds = [0, -2],
            });

            Assert.Single(q.Items);
            Assert.Equal(1, q.Items[0].ProductId);
        }
    }

    [Fact]
    public async Task Update_title_amount_notes_status()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = CreateSvc(uow, audit);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                Title = "Old",
                ProductIds = [1],
            });

            var updated = await svc.UpdateAsync(q.Id, actorId: 3, new UpdateQuotationDto
            {
                Title = "New",
                Amount = 50_000,
                Notes = "n1",
                Status = "Rejected",
            });

            Assert.Equal("New", updated.Title);
            Assert.Equal(50_000, updated.Amount);
            Assert.Equal("n1", updated.Notes);
            Assert.Equal("Rejected", updated.Status);

            var entity = await db.Quotations.FindAsync(q.Id);
            Assert.Equal(3, entity!.ApprovedById);
            audit.Verify(a => a.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()), Times.Never);
        }
    }

    [Fact]
    public async Task Update_Approved_sets_ApprovedById_and_audits_only_on_transition()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = CreateSvc(uow, audit);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                ProductIds = [1],
            });

            var approved = await svc.UpdateAsync(q.Id, actorId: 3, new UpdateQuotationDto
            {
                Status = "Approved",
            });
            Assert.Equal("Approved", approved.Status);
            Assert.Equal(3, (await db.Quotations.FindAsync(q.Id))!.ApprovedById);
            audit.Verify(a => a.LogAsync(
                "APPROVE_QUOTATION",
                "Quotation",
                q.Id.ToString(),
                It.IsAny<string?>(),
                3), Times.Once);

            await svc.UpdateAsync(q.Id, actorId: 4, new UpdateQuotationDto
            {
                Status = "Approved",
                Title = "Still approved",
            });
            audit.Verify(a => a.LogAsync(
                "APPROVE_QUOTATION",
                "Quotation",
                q.Id.ToString(),
                It.IsAny<string?>(),
                It.IsAny<int?>()), Times.Once);
        }
    }

    [Fact]
    public async Task Update_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(999, 2, new UpdateQuotationDto { Title = "x" }));
        }
    }

    [Fact]
    public async Task Update_Amount_lte_0_and_blank_status_ignored()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = CreateSvc(uow);

            var q = await svc.CreateAsync(2, new CreateQuotationDto
            {
                CustomerId = 1,
                ProductIds = [1],
                Amount = 12_000,
                Status = "PendingApproval",
            });

            var updated = await svc.UpdateAsync(q.Id, 2, new UpdateQuotationDto
            {
                Amount = 0,
                Status = "  ",
                Note = "via-note",
            });

            Assert.Equal(12_000, updated.Amount);
            Assert.Equal("PendingApproval", updated.Status);
            Assert.Equal("via-note", updated.Notes);
        }
    }
}
