using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class CategoryServiceTests
{
    [Fact]
    public async Task Create_list_and_soft_delete()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CategoryService(uow);

            var created = await svc.CreateDtoAsync(new CategoryUpsertDto
            {
                Name = "Bàn",
                Description = "Bàn gỗ",
            });
            Assert.True(created.Id > 0);

            var all = await svc.GetDtosAsync();
            Assert.Contains(all, c => c.Name == "Bàn");

            await svc.DeleteAsync(created.Id);

            var after = await svc.GetDtosAsync();
            Assert.DoesNotContain(after, c => c.Id == created.Id);
        }
    }

    [Fact]
    public async Task GetDtos_excludes_inactive()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CategoryService(uow);

            var created = await svc.CreateDtoAsync(new CategoryUpsertDto { Name = "Hidden" });
            await svc.DeleteAsync(created.Id);

            var list = await svc.GetDtosAsync();
            Assert.DoesNotContain(list, c => c.Id == created.Id);
            Assert.Contains(list, c => c.Id == 1);
        }
    }

    [Fact]
    public async Task UpdateDtoAsync_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CategoryService(uow);

            var updated = await svc.UpdateDtoAsync(1, new CategoryUpsertDto
            {
                Name = "  Sofa Premium  ",
                Description = "updated",
            });

            Assert.Equal("Sofa Premium", updated.Name);
            Assert.Equal("updated", updated.Description);
        }
    }

    [Fact]
    public async Task UpdateDtoAsync_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CategoryService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateDtoAsync(999, new CategoryUpsertDto { Name = "X" }));
        }
    }

    [Fact]
    public async Task Delete_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CategoryService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAsync(999));
        }
    }

    [Fact]
    public async Task Create_trims_name()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new CategoryService(uow);

            var created = await svc.CreateDtoAsync(new CategoryUpsertDto
            {
                Name = "  Kệ TV  ",
                Description = "desc",
            });

            Assert.Equal("Kệ TV", created.Name);
        }
    }
}
