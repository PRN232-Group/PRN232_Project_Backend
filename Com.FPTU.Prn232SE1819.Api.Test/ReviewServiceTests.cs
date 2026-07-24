using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Services;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class ReviewServiceTests
{
    [Fact]
    public async Task GetByProduct_empty()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ReviewService(uow);

            var list = await svc.GetByProductAsync(1);
            Assert.Empty(list);
        }
    }

    [Fact]
    public async Task GetByProduct_ordered_by_CreatedAt_desc()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var older = DateTime.UtcNow.AddHours(-2);
            var newer = DateTime.UtcNow.AddHours(-1);
            db.Set<ProductReview>().AddRange(
                new ProductReview
                {
                    ProductId = 1,
                    UserId = 1,
                    Rating = 3,
                    Comment = "older",
                    CreatedAt = older,
                },
                new ProductReview
                {
                    ProductId = 1,
                    UserId = 2,
                    Rating = 5,
                    Comment = "newer",
                    CreatedAt = newer,
                });
            await db.SaveChangesAsync();

            var svc = new ReviewService(uow);
            var list = await svc.GetByProductAsync(1);
            Assert.Equal(2, list.Count);
            Assert.Equal("newer", list[0].Comment);
            Assert.Equal("older", list[1].Comment);
        }
    }

    [Fact]
    public async Task Create_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ReviewService(uow);

            var created = await svc.CreateAsync(1, 1, new CreateReviewDto
            {
                Rating = 4,
                Comment = "Nice sofa",
            });

            Assert.True(created.Id > 0);
            Assert.Equal(1, created.ProductId);
            Assert.Equal(1, created.UserId);
            Assert.Equal(4, created.Rating);
            Assert.Equal("Nice sofa", created.Comment);
            Assert.Equal("Khach Hang", created.UserName);
        }
    }

    [Fact]
    public async Task Create_rating_less_than_1_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ReviewService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, 1, new CreateReviewDto { Rating = 0 }));
        }
    }

    [Fact]
    public async Task Create_rating_greater_than_5_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ReviewService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, 1, new CreateReviewDto { Rating = 6 }));
        }
    }

    [Fact]
    public async Task Create_product_missing_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ReviewService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.CreateAsync(999, 1, new CreateReviewDto { Rating = 5 }));
        }
    }

    [Fact]
    public async Task Create_product_deleted_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var product = await db.Products.FindAsync(1);
            product!.IsDeleted = true;
            await db.SaveChangesAsync();

            var svc = new ReviewService(uow);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.CreateAsync(1, 1, new CreateReviewDto { Rating = 5 }));
        }
    }

    [Fact]
    public async Task Create_duplicate_review_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ReviewService(uow);

            await svc.CreateAsync(1, 1, new CreateReviewDto { Rating = 4, Comment = "first" });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(1, 1, new CreateReviewDto { Rating = 5, Comment = "second" }));
        }
    }
}
