using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
using Com.FPTU.Prn232SE1819.Api.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class ContentServiceTests
{
    private static async Task SeedContentsAsync(InteriorStudioDbContext db)
    {
        db.Set<Content>().AddRange(
            new Content
            {
                Title = "Published Post",
                Slug = "published-post",
                Type = "Blog",
                Body = "body1",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            },
            new Content
            {
                Title = "Draft Post",
                Slug = "draft-post",
                Type = "News",
                Body = "body2",
                IsPublished = false,
                PublishedAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
            });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAll_publishedOnly_true()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);

            var list = await svc.GetAllAsync(publishedOnly: true);
            Assert.Single(list);
            Assert.All(list, c => Assert.True(c.IsPublished));
        }
    }

    [Fact]
    public async Task GetAll_publishedOnly_false()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);

            var list = await svc.GetAllAsync(publishedOnly: false);
            Assert.Equal(2, list.Count);
        }
    }

    [Fact]
    public async Task GetById_found()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);
            var id = db.Set<Content>().First(c => c.Slug == "published-post").Id;

            var got = await svc.GetByIdAsync(id);
            Assert.NotNull(got);
            Assert.Equal("Published Post", got!.Title);
        }
    }

    [Fact]
    public async Task GetById_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            Assert.Null(await svc.GetByIdAsync(999));
        }
    }

    [Fact]
    public async Task GetBySlug_published_case_insensitive()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);

            var got = await svc.GetBySlugAsync("  Published-Post  ");
            Assert.NotNull(got);
            Assert.Equal("published-post", got!.Slug);
        }
    }

    [Fact]
    public async Task GetBySlug_unpublished_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);

            Assert.Null(await svc.GetBySlugAsync("draft-post"));
        }
    }

    [Fact]
    public async Task GetBySlug_blank_returns_null()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            Assert.Null(await svc.GetBySlugAsync("   "));
            Assert.Null(await svc.GetBySlugAsync(null!));
        }
    }

    [Fact]
    public async Task Create_published()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            var created = await svc.CreateAsync(new ContentUpsertDto
            {
                Title = "  New Post  ",
                Slug = "  new-post  ",
                Type = "Guide",
                Body = "hello",
                IsPublished = true,
            });

            Assert.True(created.Id > 0);
            Assert.Equal("New Post", created.Title);
            Assert.Equal("new-post", created.Slug);
            Assert.Equal("Guide", created.Type);
            Assert.True(created.IsPublished);
            Assert.NotNull(created.PublishedAt);
        }
    }

    [Fact]
    public async Task Create_unpublished_and_default_Type_Blog()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            var created = await svc.CreateAsync(new ContentUpsertDto
            {
                Title = "Draft",
                Slug = "draft-1",
                Type = "  ",
                IsPublished = false,
            });

            Assert.Equal("Blog", created.Type);
            Assert.False(created.IsPublished);
            Assert.Null(created.PublishedAt);
        }
    }

    [Fact]
    public async Task Create_missing_title_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new ContentUpsertDto
                {
                    Title = "  ",
                    Slug = "ok-slug",
                }));
        }
    }

    [Fact]
    public async Task Create_missing_slug_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new ContentUpsertDto
                {
                    Title = "Title",
                    Slug = "  ",
                }));
        }
    }

    [Fact]
    public async Task Create_duplicate_slug_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new ContentUpsertDto
                {
                    Title = "Dup",
                    Slug = "published-post",
                }));
        }
    }

    [Fact]
    public async Task Update_success()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);
            var id = db.Set<Content>().First(c => c.Slug == "published-post").Id;

            var updated = await svc.UpdateAsync(id, new ContentUpsertDto
            {
                Title = "  Renamed  ",
                Slug = "renamed-post",
                Type = "News",
                Body = "updated body",
                IsPublished = true,
            });

            Assert.Equal("Renamed", updated.Title);
            Assert.Equal("renamed-post", updated.Slug);
            Assert.Equal("News", updated.Type);
            Assert.Equal("updated body", updated.Body);
        }
    }

    [Fact]
    public async Task Update_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(999, new ContentUpsertDto
                {
                    Title = "X",
                    Slug = "x",
                }));
        }
    }

    [Fact]
    public async Task Update_duplicate_slug_exclude_self_ok_and_conflict()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);
            var published = db.Set<Content>().First(c => c.Slug == "published-post");
            var draft = db.Set<Content>().First(c => c.Slug == "draft-post");

            var sameSlug = await svc.UpdateAsync(published.Id, new ContentUpsertDto
            {
                Title = published.Title,
                Slug = "published-post",
                Type = published.Type,
                IsPublished = true,
            });
            Assert.Equal("published-post", sameSlug.Slug);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateAsync(draft.Id, new ContentUpsertDto
                {
                    Title = "Clash",
                    Slug = "published-post",
                    IsPublished = false,
                }));
        }
    }

    [Fact]
    public async Task Update_unpublish_clears_PublishedAt()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);
            var id = db.Set<Content>().First(c => c.Slug == "published-post").Id;

            var updated = await svc.UpdateAsync(id, new ContentUpsertDto
            {
                Title = "Published Post",
                Slug = "published-post",
                IsPublished = false,
            });

            Assert.False(updated.IsPublished);
            Assert.Null(updated.PublishedAt);
        }
    }

    [Fact]
    public async Task Update_republish_sets_PublishedAt()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);
            var id = db.Set<Content>().First(c => c.Slug == "draft-post").Id;

            var updated = await svc.UpdateAsync(id, new ContentUpsertDto
            {
                Title = "Draft Post",
                Slug = "draft-post",
                Type = "News",
                IsPublished = true,
            });

            Assert.True(updated.IsPublished);
            Assert.NotNull(updated.PublishedAt);
        }
    }

    [Fact]
    public async Task Delete_hard_removes_entity()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            await SeedContentsAsync(db);
            var svc = new ContentService(uow);
            var id = db.Set<Content>().First(c => c.Slug == "draft-post").Id;

            await svc.DeleteAsync(id);

            Assert.Null(await db.Set<Content>().FindAsync(id));
            Assert.Null(await svc.GetByIdAsync(id));
        }
    }

    [Fact]
    public async Task Delete_not_found_throws()
    {
        var (db, uow, sp) = TestDb.Create(withAudit: false);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);
            var svc = new ContentService(uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAsync(999));
        }
    }
}
