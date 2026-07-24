using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Services.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class UserServiceTests
{
    private static (UserService Svc, Mock<IAuditService> Audit) CreateSvc(
        Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories.IUnitOfWork uow)
    {
        var audit = new Mock<IAuditService>();
        return (new UserService(uow, audit.Object), audit);
    }

    [Fact]
    public async Task GetProfile_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            var profile = await svc.GetProfileAsync(1);
            Assert.NotNull(profile);
            Assert.Equal("kh@test.com", profile!.Email);
            Assert.Equal("Khach Hang", profile.FullName);
            Assert.Equal("Customer", profile.Role);
        }
    }

    [Fact]
    public async Task GetProfile_not_found_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            Assert.Null(await svc.GetProfileAsync(999));
        }
    }

    [Fact]
    public async Task GetProfile_soft_deleted_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var user = await db.Users.FindAsync(1);
            user!.IsDeleted = true;
            await db.SaveChangesAsync();

            var (svc, _) = CreateSvc(uow);
            Assert.Null(await svc.GetProfileAsync(1));
        }
    }

    [Fact]
    public async Task UpdateProfile_success_with_FullName()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            var updated = await svc.UpdateProfileAsync(1, new UpdateProfileDto
            {
                FullName = "  Nguyen Van A  ",
                Phone = "090111",
                Address = "HN",
            });

            Assert.NotNull(updated);
            Assert.Equal("Nguyen Van A", updated!.FullName);
            Assert.Equal("090111", updated.Phone);
            Assert.Equal("HN", updated.Address);
        }
    }

    [Fact]
    public async Task UpdateProfile_uses_Name_fallback()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            var updated = await svc.UpdateProfileAsync(1, new UpdateProfileDto
            {
                Name = "  Fallback Name  ",
            });

            Assert.NotNull(updated);
            Assert.Equal("Fallback Name", updated!.FullName);
        }
    }

    [Fact]
    public async Task UpdateProfile_missing_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            Assert.Null(await svc.UpdateProfileAsync(999, new UpdateProfileDto { FullName = "X" }));
        }
    }

    [Fact]
    public async Task GetAll_excludes_deleted()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var user = await db.Users.FindAsync(1);
            user!.IsDeleted = true;
            await db.SaveChangesAsync();

            var (svc, _) = CreateSvc(uow);
            var all = await svc.GetAllAsync();
            Assert.DoesNotContain(all, u => u.Id == 1);
            Assert.Equal(3, all.Count);
        }
    }

    [Fact]
    public async Task CreateAsync_Admin_creates_Customer()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit) = CreateSvc(uow);

            var created = await svc.CreateAsync(new CreateUserDto
            {
                Name = "New Customer",
                Email = "NewCust@Test.com",
                Role = "Customer",
                Password = "Secret@123",
            }, actorId: 4);

            Assert.True(created.Id > 0);
            Assert.Equal("newcust@test.com", created.Email);
            Assert.Equal("Customer", created.Role);
            Assert.Equal("New Customer", created.Name);
            audit.Verify(a => a.LogAsync(
                "CREATE_USER",
                "User",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                4), Times.Once);
        }
    }

    [Fact]
    public async Task CreateAsync_Admin_creates_Sales()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            var created = await svc.CreateAsync(new CreateUserDto
            {
                Name = "New Sales",
                Email = "newsales@test.com",
                Role = "Sales",
            }, actorId: 4);

            Assert.Equal("Sales", created.Role);
        }
    }

    [Fact]
    public async Task CreateAsync_non_Admin_unauthorized()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.CreateAsync(new CreateUserDto
                {
                    Name = "X",
                    Email = "x@test.com",
                    Role = "Customer",
                }, actorId: 2));
        }
    }

    [Fact]
    public async Task CreateAsync_actor_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.CreateAsync(new CreateUserDto
                {
                    Name = "X",
                    Email = "x@test.com",
                    Role = "Customer",
                }, actorId: 999));
        }
    }

    [Fact]
    public async Task CreateAsync_cannot_create_Admin()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new CreateUserDto
                {
                    Name = "Boss",
                    Email = "boss@test.com",
                    Role = "Admin",
                }, actorId: 4));
        }
    }

    [Fact]
    public async Task CreateAsync_email_exists()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new CreateUserDto
                {
                    Name = "Dup",
                    Email = "kh@test.com",
                    Role = "Customer",
                }, actorId: 4));
        }
    }

    [Fact]
    public async Task CreateAsync_role_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new CreateUserDto
                {
                    Name = "X",
                    Email = "ghost@test.com",
                    Role = "Ghost",
                }, actorId: 4));
        }
    }

    [Fact]
    public async Task CreateAsync_default_password_ChangeMe123()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            var created = await svc.CreateAsync(new CreateUserDto
            {
                Name = "Default Pwd",
                Email = "defpwd@test.com",
                Role = "Customer",
                Password = "   ",
            }, actorId: 4);

            var entity = await db.Users.AsNoTracking().FirstAsync(u => u.Id == created.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("ChangeMe@123", entity.PasswordHash));
        }
    }

    [Fact]
    public async Task UpdateRole_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit) = CreateSvc(uow);

            var updated = await svc.UpdateRoleAsync(1, "Sales", actorId: 4);
            Assert.Equal("Sales", updated.Role);
            audit.Verify(a => a.LogAsync(
                "UPDATE_USER_ROLE",
                "User",
                "1",
                It.IsAny<string?>(),
                4), Times.Once);
        }
    }

    [Fact]
    public async Task UpdateRole_self_change_blocked()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateRoleAsync(4, "Manager", actorId: 4));
        }
    }

    [Fact]
    public async Task UpdateRole_cannot_manage_equal_or_higher()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateRoleAsync(4, "Sales", actorId: 3));
        }
    }

    [Fact]
    public async Task UpdateRole_target_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateRoleAsync(999, "Sales", actorId: 4));
        }
    }

    [Fact]
    public async Task UpdateRole_role_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateRoleAsync(1, "Ghost", actorId: 4));
        }
    }

    [Fact]
    public async Task UpdateRole_cannot_assign_Admin()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateRoleAsync(1, "Admin", actorId: 4));
        }
    }

    [Fact]
    public async Task SetLocked_lock_and_unlock()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, audit) = CreateSvc(uow);

            var locked = await svc.SetLockedAsync(1, true, actorId: 4);
            Assert.True(locked.IsLocked);
            Assert.Equal("Locked", locked.Status);

            var unlocked = await svc.SetLockedAsync(1, false, actorId: 4);
            Assert.False(unlocked.IsLocked);
            Assert.Equal("Active", unlocked.Status);

            audit.Verify(a => a.LogAsync(
                "LOCK_USER",
                "User",
                "1",
                It.IsAny<string?>(),
                4), Times.Exactly(2));
        }
    }

    [Fact]
    public async Task SetLocked_self_change_blocked()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetLockedAsync(4, true, actorId: 4));
        }
    }

    [Fact]
    public async Task SetLocked_cannot_manage_equal_or_higher()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.SetLockedAsync(4, true, actorId: 3));
        }
    }

    [Fact]
    public async Task SetLocked_target_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.SetLockedAsync(999, true, actorId: 4));
        }
    }

    [Fact]
    public async Task SetLocked_actor_not_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var (svc, _) = CreateSvc(uow);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                svc.SetLockedAsync(1, true, actorId: 999));
        }
    }
}
