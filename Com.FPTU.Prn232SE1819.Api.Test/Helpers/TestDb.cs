using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Common;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;
using Com.FPTU.Prn232SE1819.Api.Services.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Com.FPTU.Prn232SE1819.Api.Test;

internal static class TestDb
{
    public const string DefaultPassword = "Pass@123";

    public static (InteriorStudioDbContext Db, IUnitOfWork Uow, ServiceProvider Sp) Create(
        string? name = null,
        bool withAudit = true)
    {
        var dbName = name ?? Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<InteriorStudioDbContext>((sp, opt) =>
        {
            opt.UseInMemoryDatabase(dbName);
            opt.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            if (withAudit)
                opt.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<Func<InteriorStudioDbContext>>(sp =>
            () => sp.GetRequiredService<InteriorStudioDbContext>());
        services.AddScoped<DbFactoryContext>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<InteriorStudioDbContext>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        return (db, uow, sp);
    }

    public static async Task SeedBasicsAsync(InteriorStudioDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

        db.Roles.AddRange(
            new Role { Id = 1, Name = "Customer", Description = "KH" },
            new Role { Id = 2, Name = "Sales", Description = "Sales" },
            new Role { Id = 3, Name = "Manager", Description = "Manager" },
            new Role { Id = 4, Name = "Admin", Description = "Admin" });

        db.Categories.Add(new Category
        {
            Id = 1,
            Name = "Sofa",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        db.Users.AddRange(
            new User
            {
                Id = 1,
                Email = "kh@test.com",
                PasswordHash = hash,
                FullName = "Khach Hang",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Id = 2,
                Email = "sales@test.com",
                PasswordHash = hash,
                FullName = "Sales User",
                RoleId = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Id = 3,
                Email = "manager@test.com",
                PasswordHash = hash,
                FullName = "Manager User",
                RoleId = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Id = 4,
                Email = "admin@test.com",
                PasswordHash = hash,
                FullName = "Admin User",
                RoleId = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Sofa Japandi",
                Description = "Sofa gỗ",
                Price = 10_000_000,
                MarketPrice = 12_000_000,
                Stock = 5,
                CategoryId = 1,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            },
            new Product
            {
                Id = 2,
                Name = "Bàn trà",
                Description = "Oak table",
                Price = 3_000_000,
                MarketPrice = 3_500_000,
                Stock = 10,
                CategoryId = 1,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            });

        await db.SaveChangesAsync();
    }

    public static Order NewOrder(
        int customerId,
        string status,
        decimal total,
        DateTime? createdAt = null)
        => new()
        {
            CustomerId = customerId,
            Status = status,
            TotalPrice = total,
            ShippingAddress = "HCM",
            Phone = "0900",
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
}
