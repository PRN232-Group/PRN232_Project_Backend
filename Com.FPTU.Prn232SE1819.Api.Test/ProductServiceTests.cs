using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class ProductServiceTests
{
    [Fact]
    public async Task Create_and_get_by_id()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var created = await svc.CreateAsync(new ProductUpsertDto
            {
                Name = "Ghế ăn",
                Price = 1_500_000,
                MarketPrice = 2_000_000,
                Stock = 10,
                CategoryId = 1,
                IsActive = true,
                Specs = new ProductSpecsDto
                {
                    Material = "Gỗ",
                    Dimensions = "45x45",
                },
            });

            Assert.True(created.Id > 0);
            Assert.Equal("Ghế ăn", created.Name);
            Assert.Equal("Gỗ", created.Specs?.Material);

            var got = await svc.GetByIdAsync(created.Id);
            Assert.NotNull(got);
            Assert.Equal(created.Id, got!.Id);
        }
    }

    [Fact]
    public async Task Soft_delete_hides_from_get()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await svc.DeleteAsync(1);
            var got = await svc.GetByIdAsync(1);
            Assert.Null(got);

            var all = await svc.GetAllAsync();
            Assert.DoesNotContain(all, p => p.Id == 1);
        }
    }

    [Fact]
    public async Task UpdatePrice_changes_price()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var updated = await svc.UpdatePriceAsync(1, 9_000_000);
            Assert.Equal(9_000_000, updated.Price);
        }
    }

    [Fact]
    public async Task GetAll_excludes_deleted()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var before = await svc.GetAllAsync();
            Assert.Equal(2, before.Count);

            await svc.DeleteAsync(2);
            var after = await svc.GetAllAsync();
            Assert.Single(after);
            Assert.DoesNotContain(after, p => p.Id == 2);
            Assert.Contains(after, p => p.Id == 1);
        }
    }

    [Fact]
    public async Task GetById_not_found_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            Assert.Null(await svc.GetByIdAsync(999));
        }
    }

    [Fact]
    public async Task GetById_deleted_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await svc.DeleteAsync(1);
            Assert.Null(await svc.GetByIdAsync(1));
        }
    }

    [Fact]
    public async Task Search_null_or_empty_returns_all()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var fromNull = await svc.SearchAsync(null);
            var fromEmpty = await svc.SearchAsync("   ");
            Assert.Equal(2, fromNull.Count);
            Assert.Equal(2, fromEmpty.Count);
        }
    }

    [Fact]
    public async Task Search_matches_name()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var result = await svc.SearchAsync("Japandi");
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }
    }

    [Fact]
    public async Task Search_matches_description()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var result = await svc.SearchAsync("Oak");
            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }
    }

    [Fact]
    public async Task Search_no_match_returns_empty()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var result = await svc.SearchAsync("xyz-not-found");
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task Create_without_specs()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var created = await svc.CreateAsync(new ProductUpsertDto
            {
                Name = "Đèn sàn",
                Price = 500_000,
                Stock = 3,
                CategoryId = 1,
                IsActive = true,
            });

            Assert.True(created.Id > 0);
            Assert.Null(created.Specs);
            Assert.Null(db.Set<ProductSpec>().FirstOrDefault(s => s.ProductId == created.Id));
        }
    }

    [Fact]
    public async Task Update_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            var updated = await svc.UpdateAsync(1, new ProductUpsertDto
            {
                Name = "  Sofa Updated  ",
                Description = "new desc",
                Price = 11_000_000,
                MarketPrice = 13_000_000,
                Stock = 8,
                CategoryId = 1,
                IsActive = true,
            });

            Assert.Equal("Sofa Updated", updated.Name);
            Assert.Equal("new desc", updated.Description);
            Assert.Equal(11_000_000, updated.Price);
            Assert.Equal(8, updated.Stock);
        }
    }

    [Fact]
    public async Task Update_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(999, new ProductUpsertDto
                {
                    Name = "X",
                    Price = 1,
                    Stock = 1,
                    CategoryId = 1,
                }));
        }
    }

    [Fact]
    public async Task Update_inserts_specs_when_none()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            Assert.Null(db.Set<ProductSpec>().FirstOrDefault(s => s.ProductId == 1));

            var updated = await svc.UpdateAsync(1, new ProductUpsertDto
            {
                Name = "Sofa Japandi",
                Price = 10_000_000,
                Stock = 5,
                CategoryId = 1,
                IsActive = true,
                Specs = new ProductSpecsDto
                {
                    Material = "Vải",
                    Dimensions = "200x90",
                    Origin = "VN",
                },
            });

            Assert.Equal("Vải", updated.Specs?.Material);
            Assert.Equal("200x90", updated.Specs?.Dimensions);
            Assert.NotNull(db.Set<ProductSpec>().FirstOrDefault(s => s.ProductId == 1));
        }
    }

    [Fact]
    public async Task Update_updates_existing_specs()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            db.Set<ProductSpec>().Add(new ProductSpec
            {
                ProductId = 1,
                Material = "Old",
                Dimensions = "1x1",
            });
            await db.SaveChangesAsync();

            var svc = new ProductService(uow);
            var updated = await svc.UpdateAsync(1, new ProductUpsertDto
            {
                Name = "Sofa Japandi",
                Price = 10_000_000,
                Stock = 5,
                CategoryId = 1,
                IsActive = true,
                Specs = new ProductSpecsDto
                {
                    Material = "New Material",
                    Dimensions = "3x3",
                    Finish = "Matte",
                },
            });

            Assert.Equal("New Material", updated.Specs?.Material);
            Assert.Equal("3x3", updated.Specs?.Dimensions);
            Assert.Equal("Matte", updated.Specs?.Finish);
            Assert.Equal(1, db.Set<ProductSpec>().Count(s => s.ProductId == 1));
        }
    }

    [Fact]
    public async Task UpdatePrice_negative_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdatePriceAsync(1, -1));
        }
    }

    [Fact]
    public async Task UpdatePrice_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdatePriceAsync(999, 100));
        }
    }

    [Fact]
    public async Task UpdatePrice_deleted_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await svc.DeleteAsync(1);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdatePriceAsync(1, 100));
        }
    }

    [Fact]
    public async Task Delete_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ProductService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAsync(999));
        }
    }
}
