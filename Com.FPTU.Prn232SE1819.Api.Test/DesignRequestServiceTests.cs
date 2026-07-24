using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class DesignRequestServiceTests
{
    [Fact]
    public async Task Create_sets_New_status_and_returns_mine()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new DesignRequestService(db, audit.Object);

            var created = await svc.CreateAsync(1, new CreateDesignRequestDto
            {
                Title = "Căn hộ 2PN",
                Style = "Japandi",
                Budget = 80_000_000,
                Notes = "test",
            });

            Assert.Equal("New", created.Status);
            Assert.Equal("Căn hộ 2PN", created.Title);
            audit.Verify(a => a.LogAsync(
                "CREATE_DESIGN_REQUEST",
                "DesignRequest",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                1), Times.Once);

            var mine = await svc.GetMineAsync(1);
            Assert.Single(mine);
        }
    }

    [Fact]
    public async Task UpdateStatus_follows_forward_flow()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new DesignRequestService(db, audit.Object);

            var created = await svc.CreateAsync(1, new CreateDesignRequestDto
            {
                Title = "Flow",
            });

            var r1 = await svc.UpdateStatusAsync(created.Id, "InReview", actorUserId: 1);
            Assert.Equal("InReview", r1.Status);

            var r2 = await svc.UpdateStatusAsync(created.Id, "Quoted", actorUserId: 1);
            Assert.Equal("Quoted", r2.Status);

            var r3 = await svc.UpdateStatusAsync(created.Id, "Done", actorUserId: 1);
            Assert.Equal("Done", r3.Status);
        }
    }

    [Fact]
    public async Task GetById_customer_cannot_read_others()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Users.Add(new User
            {
                Id = 5,
                Email = "other@test.com",
                PasswordHash = "x",
                FullName = "Other",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var audit = new Mock<IAuditService>();
            var svc = new DesignRequestService(db, audit.Object);
            var created = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Private" });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.GetByIdAsync(created.Id, currentUserId: 5, role: "Customer"));
        }
    }

    [Fact]
    public async Task UpdateStatus_rejects_invalid_jump()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new DesignRequestService(db, audit.Object);
            var created = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Jump" });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateStatusAsync(created.Id, "Done"));
        }
    }

    [Fact]
    public async Task Create_RelatedProductIds_distinct_and_Attachments_skip_blank()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new DesignRequestService(db, Mock.Of<IAuditService>());

            var created = await svc.CreateAsync(1, new CreateDesignRequestDto
            {
                Title = "With kids",
                RelatedProductIds = [1, 1, 2, 2],
                Attachments = ["https://a.jpg", "  ", "", "https://b.jpg"],
            });

            Assert.Equal(2, created.RelatedProductIds.Count);
            Assert.Contains(1, created.RelatedProductIds);
            Assert.Contains(2, created.RelatedProductIds);
            Assert.Equal(2, created.Attachments.Count);
            Assert.DoesNotContain(created.Attachments, a => string.IsNullOrWhiteSpace(a));
        }
    }

    [Fact]
    public async Task GetAll_ordered_by_UpdatedAt_desc()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new DesignRequestService(db, Mock.Of<IAuditService>());

            var older = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Older" });
            await Task.Delay(20);
            var newer = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Newer" });

            var entity = await db.DesignRequests.FindAsync(older.Id);
            entity!.UpdatedAt = DateTime.UtcNow.AddDays(-2);
            await db.SaveChangesAsync();

            var all = await svc.GetAllAsync();
            Assert.Equal(2, all.Count);
            Assert.Equal(newer.Id, all[0].Id);
            Assert.Equal(older.Id, all[1].Id);
        }
    }

    [Fact]
    public async Task GetMine_empty_for_other_user()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new DesignRequestService(db, Mock.Of<IAuditService>());
            await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Only1" });

            Assert.Empty(await svc.GetMineAsync(2));
        }
    }

    [Fact]
    public async Task GetById_null_owner_ok_staff_Sales_ok()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new DesignRequestService(db, Mock.Of<IAuditService>());
            var created = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "X" });

            Assert.Null(await svc.GetByIdAsync(999, 1, "Customer"));

            var owner = await svc.GetByIdAsync(created.Id, 1, "Customer");
            Assert.NotNull(owner);
            Assert.Equal(created.Id, owner!.Id);

            var sales = await svc.GetByIdAsync(created.Id, 2, "Sales");
            Assert.NotNull(sales);
            Assert.Equal(created.Id, sales!.Id);
        }
    }

    [Fact]
    public async Task UpdateStatus_not_found_throws()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new DesignRequestService(db, Mock.Of<IAuditService>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateStatusAsync(999, "InReview"));
        }
    }

    [Fact]
    public async Task UpdateStatus_Done_cannot_advance()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new DesignRequestService(db, Mock.Of<IAuditService>());
            var created = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Done" });
            await svc.UpdateStatusAsync(created.Id, "InReview");
            await svc.UpdateStatusAsync(created.Id, "Quoted");
            await svc.UpdateStatusAsync(created.Id, "Done");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateStatusAsync(created.Id, "InReview"));
        }
    }

    [Fact]
    public async Task UpdateStatus_case_insensitive_InReview_and_audits()
    {
        var (db, _, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new DesignRequestService(db, audit.Object);
            var created = await svc.CreateAsync(1, new CreateDesignRequestDto { Title = "Case" });

            var updated = await svc.UpdateStatusAsync(created.Id, "inreview", actorUserId: 2);
            Assert.Equal("InReview", updated.Status);

            audit.Verify(a => a.LogAsync(
                "UPDATE_DESIGN_REQUEST_STATUS",
                "DesignRequest",
                created.Id.ToString(),
                It.Is<string?>(d => d != null && d.Contains("New → InReview")),
                2), Times.Once);
        }
    }
}
