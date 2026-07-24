using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class PermissionServiceTests
{
    private static async Task SeedPagesAsync(InteriorStudioDbContext db)
    {
        db.AppPages.AddRange(
            new AppPage
            {
                Id = 1,
                PageKey = "dashboard",
                Name = "Dashboard",
                Section = "main",
                SortOrder = 1,
                IsActive = true,
            },
            new AppPage
            {
                Id = 2,
                PageKey = "orders",
                Name = "Orders",
                Section = "main",
                SortOrder = 2,
                IsActive = true,
            },
            new AppPage
            {
                Id = 3,
                PageKey = "inactive-page",
                Name = "Inactive",
                Section = "main",
                SortOrder = 99,
                IsActive = false,
            });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMatrix_excludes_Production()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Roles.Add(new Role { Id = 5, Name = "Production", Description = "Prod" });
            await SeedPagesAsync(db);

            var svc = new PermissionService(uow, Mock.Of<IAuditService>());
            var matrix = await svc.GetMatrixAsync();

            Assert.DoesNotContain(matrix.Roles, r => r.Name == "Production");
            Assert.Contains(matrix.Roles, r => r.Name == "Sales");
            Assert.Equal(2, matrix.Pages.Count);
            Assert.DoesNotContain(matrix.Pages, p => p.PageKey == "inactive-page");
        }
    }

    [Fact]
    public async Task SetRolePermissions_Sales_success_and_audit()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedPagesAsync(db);
            var audit = new Mock<IAuditService>();
            var svc = new PermissionService(uow, audit.Object);

            await svc.SetRolePermissionsAsync(2, new SetRolePermissionsDto
            {
                PageKeys = ["dashboard", "orders"],
            }, actorUserId: 4);

            var keys = await svc.GetPageKeysForRoleAsync(2);
            Assert.Equal(["dashboard", "orders"], keys.ToList());

            audit.Verify(a => a.LogAsync(
                "UPDATE_PERMISSIONS",
                "RolePermission",
                "2",
                It.IsAny<string?>(),
                4), Times.Once);
        }
    }

    [Fact]
    public async Task SetRolePermissions_blocks_Admin_Customer_Production()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Roles.Add(new Role { Id = 5, Name = "Production", Description = "Prod" });
            await SeedPagesAsync(db);
            var svc = new PermissionService(uow, Mock.Of<IAuditService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetRolePermissionsAsync(4, new SetRolePermissionsDto { PageKeys = ["dashboard"] }, 4));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetRolePermissionsAsync(1, new SetRolePermissionsDto { PageKeys = ["dashboard"] }, 4));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetRolePermissionsAsync(5, new SetRolePermissionsDto { PageKeys = ["dashboard"] }, 4));
        }
    }

    [Fact]
    public async Task SetRolePermissions_role_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new PermissionService(uow, Mock.Of<IAuditService>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.SetRolePermissionsAsync(99, new SetRolePermissionsDto { PageKeys = ["dashboard"] }, 4));
        }
    }

    [Fact]
    public async Task SetRolePermissions_blank_keys_ignored_unknown_not_inserted()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedPagesAsync(db);
            var svc = new PermissionService(uow, Mock.Of<IAuditService>());

            await svc.SetRolePermissionsAsync(2, new SetRolePermissionsDto
            {
                PageKeys = ["dashboard", "  ", "", "no-such-page", "orders"],
            }, actorUserId: 4);

            var keys = await svc.GetPageKeysForRoleAsync(2);
            Assert.Equal(2, keys.Count);
            Assert.DoesNotContain("no-such-page", keys);
            Assert.Equal(2, db.RolePermissions.Count(rp => rp.RoleId == 2));
        }
    }

    [Fact]
    public async Task GetPageKeysForRoleAsync_returns_active_grants()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedPagesAsync(db);
            db.RolePermissions.AddRange(
                new RolePermission { RoleId = 3, PageId = 1 },
                new RolePermission { RoleId = 3, PageId = 2 },
                new RolePermission { RoleId = 3, PageId = 3 });
            await db.SaveChangesAsync();

            var svc = new PermissionService(uow, Mock.Of<IAuditService>());
            var keys = await svc.GetPageKeysForRoleAsync(3);

            Assert.Equal(["dashboard", "orders"], keys.ToList());
        }
    }
}
