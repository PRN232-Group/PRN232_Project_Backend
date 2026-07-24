using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class InteriorDesignServiceTests
{
    [Fact]
    public async Task GetAll_published_vs_includeUnpublished()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            await svc.CreateAsync(new InteriorDesignUpsertDto
            {
                Title = "Published",
                IsPublished = true,
            });
            await svc.CreateAsync(new InteriorDesignUpsertDto
            {
                Title = "Draft",
                IsPublished = false,
            });

            var pub = await svc.GetAllAsync(includeUnpublished: false);
            Assert.Single(pub);
            Assert.Equal("Published", pub[0].Title);

            var all = await svc.GetAllAsync(includeUnpublished: true);
            Assert.Equal(2, all.Count);
        }
    }

    [Fact]
    public async Task GetById_unpublished_hidden_and_related_products_skip_deleted()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            var draft = await svc.CreateAsync(new InteriorDesignUpsertDto
            {
                Title = "Hidden",
                IsPublished = false,
                RelatedProductIds = [1, 2],
            });

            Assert.Null(await svc.GetByIdAsync(draft.Id, includeUnpublished: false));

            var p2 = await db.Products.FindAsync(2);
            p2!.IsDeleted = true;
            await db.SaveChangesAsync();

            var detail = await svc.GetByIdAsync(draft.Id, includeUnpublished: true);
            Assert.NotNull(detail);
            Assert.Contains(1, detail!.RelatedProductIds);
            Assert.Contains(2, detail.RelatedProductIds);
            Assert.Single(detail.RelatedProducts);
            Assert.Equal(1, detail.RelatedProducts[0].Id);
        }
    }

    [Fact]
    public async Task Create_with_children()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            var created = await svc.CreateAsync(new InteriorDesignUpsertDto
            {
                Title = "Japandi Living",
                Category = "Living",
                Style = "Japandi",
                Description = "desc",
                AreaSqm = 40,
                BudgetFrom = 50,
                BudgetTo = 100,
                TimelineWeeks = 8,
                ImageUrl = "https://img/main.jpg",
                Gallery = ["https://img/g1.jpg", "https://img/g2.jpg"],
                Highlights = ["H1", "  ", "H2"],
                Specs =
                [
                    new DesignSpecItemDto { Label = "Area", Value = "40m2" },
                    new DesignSpecItemDto { Label = "  ", Value = "x" },
                ],
                Materials =
                [
                    new DesignMaterialItemDto { Name = "Oak", Origin = "VN" },
                    new DesignMaterialItemDto { Name = "  " },
                ],
                Packages =
                [
                    new DesignPackageItemDto { Name = "Basic", Price = 10, Includes = "A" },
                ],
                PriceCompare = new DesignPriceCompareDto { Studio = 80, MarketAvg = 100 },
                RelatedProductIds = [1, 1, 2],
                IsPublished = true,
            });

            Assert.True(created.Id > 0);
            Assert.Equal("Japandi Living", created.Title);
            Assert.Equal(2, created.Gallery.Count);
            Assert.Equal(2, created.Highlights.Count);
            Assert.Single(created.Specs);
            Assert.Single(created.Materials);
            Assert.Single(created.Packages);
            Assert.Equal(2, created.RelatedProductIds.Count);
            Assert.Equal(80, created.PriceCompare.Studio);
        }
    }

    [Fact]
    public async Task Create_empty_title_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new InteriorDesignUpsertDto { Title = "  " }));
        }
    }

    [Fact]
    public async Task Update_replaces_children()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            var created = await svc.CreateAsync(new InteriorDesignUpsertDto
            {
                Title = "Old",
                Highlights = ["A"],
                RelatedProductIds = [1],
                Gallery = ["https://old.jpg"],
            });

            var updated = await svc.UpdateAsync(created.Id, new InteriorDesignUpsertDto
            {
                Title = "New",
                Highlights = ["B", "C"],
                RelatedProductIds = [2],
                Gallery = ["https://new.jpg"],
                IsPublished = true,
            });

            Assert.Equal("New", updated.Title);
            Assert.Equal(["B", "C"], updated.Highlights);
            Assert.Equal([2], updated.RelatedProductIds);
            Assert.Equal(["https://new.jpg"], updated.Gallery);
        }
    }

    [Fact]
    public async Task Update_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(999, new InteriorDesignUpsertDto { Title = "X" }));
        }
    }

    [Fact]
    public async Task Delete_removes_design()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);
            var created = await svc.CreateAsync(new InteriorDesignUpsertDto
            {
                Title = "ToDelete",
                IsPublished = true,
            });

            await svc.DeleteAsync(created.Id);
            Assert.Null(await svc.GetByIdAsync(created.Id, includeUnpublished: true));
        }
    }

    [Fact]
    public async Task Delete_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new InteriorDesignService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAsync(999));
        }
    }
}
