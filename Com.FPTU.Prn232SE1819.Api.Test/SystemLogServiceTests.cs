using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class SystemLogServiceTests
{
    [Fact]
    public async Task LogAsync_writes_entry()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new SystemLogService(uow);

            await svc.LogAsync("CREATE_ORDER", "Order", "1", "detail", actorUserId: 1);

            Assert.Single(db.SystemLogs);
            var log = db.SystemLogs.Single();
            Assert.Equal("CREATE_ORDER", log.Action);
            Assert.Equal("Order", log.Entity);
            Assert.Equal("1", log.EntityId);
            Assert.Equal(1, log.ActorUserId);
        }
    }

    [Fact]
    public async Task LogAsync_blank_action_or_entity_is_noop()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new SystemLogService(uow);

            await svc.LogAsync("  ", "Order", "1", null, 1);
            await svc.LogAsync("CREATE", "  ", "1", null, 1);
            await svc.LogAsync("", "", null, null, null);

            Assert.Empty(db.SystemLogs);
        }
    }

    [Fact]
    public async Task GetAll_paging_clamp()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new SystemLogService(uow);

            for (var i = 0; i < 5; i++)
                await svc.LogAsync($"ACT{i}", "Entity", i.ToString(), null, 1);

            var page0 = await svc.GetAllAsync(new SystemLogQueryDto { Page = 0, PageSize = 0 });
            Assert.Equal(1, page0.Page);
            Assert.Equal(20, page0.PageSize);
            Assert.Equal(5, page0.Total);

            var huge = await svc.GetAllAsync(new SystemLogQueryDto { Page = 1, PageSize = 100 });
            Assert.Equal(50, huge.PageSize);
        }
    }

    [Fact]
    public async Task GetAll_filters_Action_Entity_From_To()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new SystemLogService(uow);

            await svc.LogAsync("CREATE_ORDER", "Order", "1", null, 1);
            await svc.LogAsync("UPDATE_USER", "User", "2", null, 1);
            await svc.LogAsync("CREATE_PRODUCT", "Product", "3", null, 1);

            var mid = db.SystemLogs.First(x => x.Action == "UPDATE_USER");
            mid.CreatedAt = new DateTime(2026, 7, 10, 12, 0, 0);
            var old = db.SystemLogs.First(x => x.Action == "CREATE_ORDER");
            old.CreatedAt = new DateTime(2026, 7, 1, 12, 0, 0);
            var neu = db.SystemLogs.First(x => x.Action == "CREATE_PRODUCT");
            neu.CreatedAt = new DateTime(2026, 7, 20, 12, 0, 0);
            await db.SaveChangesAsync();

            var byAction = await svc.GetAllAsync(new SystemLogQueryDto { Action = "CREATE" });
            Assert.Equal(2, byAction.Total);

            var byEntity = await svc.GetAllAsync(new SystemLogQueryDto { Entity = "User" });
            Assert.Single(byEntity.Items);
            Assert.Equal("UPDATE_USER", byEntity.Items[0].Action);

            var ranged = await svc.GetAllAsync(new SystemLogQueryDto
            {
                From = new DateTime(2026, 7, 5),
                To = new DateTime(2026, 7, 15),
            });
            Assert.Single(ranged.Items);
            Assert.Equal("UPDATE_USER", ranged.Items[0].Action);
        }
    }

    [Fact]
    public async Task GetAll_actor_name_FullName_else_Email()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Users.Add(new User
            {
                Id = 5,
                Email = "onlyemail@test.com",
                PasswordHash = "x",
                FullName = "   ",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var svc = new SystemLogService(uow);
            await svc.LogAsync("A1", "E", "1", null, actorUserId: 1);
            await svc.LogAsync("A2", "E", "2", null, actorUserId: 5);

            var page = await svc.GetAllAsync(new SystemLogQueryDto());
            var named = page.Items.First(i => i.ActorUserId == 1);
            var emailed = page.Items.First(i => i.ActorUserId == 5);

            Assert.Equal("Khach Hang", named.ActorName);
            Assert.Equal("onlyemail@test.com", emailed.ActorName);
        }
    }
}
