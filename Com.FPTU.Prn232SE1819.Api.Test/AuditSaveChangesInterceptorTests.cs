using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Test;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task Update_product_with_spec_logs_only_product_not_spec()
    {
        var (db, _, sp) = TestDb.Create(withAudit: true);
        await using (sp)
        {
            await TestDb.SeedBasicsAsync(db);

            db.ProductSpecs.Add(new ProductSpec
            {
                ProductId = 1,
                Material = "Oak",
                Dimensions = "1x1",
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // clear noise from seed
            db.SystemLogs.RemoveRange(db.SystemLogs);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var product = await db.Products.Include(p => p.ProductSpec)
                .FirstAsync(p => p.Id == 1);
            product.IsActive = false;
            product.ProductSpec!.Material = "Walnut";
            await db.SaveChangesAsync();

            var logs = await db.SystemLogs.AsNoTracking().ToListAsync();
            Assert.DoesNotContain(logs, l =>
                l.Entity == nameof(ProductSpec)
                || (l.Action?.Contains("PRODUCT_SPEC", StringComparison.OrdinalIgnoreCase) ?? false));
            Assert.Contains(logs, l =>
                l.Entity == nameof(Product) && l.Action == "UPDATE_PRODUCT");
        }
    }

    [Fact]
    public async Task Replace_concept_children_does_not_spam_child_delete_logs()
    {
        var (db, _, sp) = TestDb.Create(withAudit: true);
        await using (sp)
        {
            var design = new InteriorDesign
            {
                Title = "Concept A",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.InteriorDesigns.Add(design);
            await db.SaveChangesAsync();

            db.InteriorDesignSpecs.AddRange(
                new InteriorDesignSpec
                {
                    InteriorDesignId = design.Id,
                    Label = "Diện tích",
                    Value = "40m2",
                    SortOrder = 0,
                },
                new InteriorDesignSpec
                {
                    InteriorDesignId = design.Id,
                    Label = "Phong cách",
                    Value = "Japandi",
                    SortOrder = 1,
                });
            db.InteriorDesignHighlights.Add(new InteriorDesignHighlight
            {
                InteriorDesignId = design.Id,
                Text = "Ánh sáng tự nhiên",
                SortOrder = 0,
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            db.SystemLogs.RemoveRange(db.SystemLogs);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var specs = await db.InteriorDesignSpecs
                .Where(s => s.InteriorDesignId == design.Id).ToListAsync();
            db.InteriorDesignSpecs.RemoveRange(specs);
            var highlights = await db.InteriorDesignHighlights
                .Where(h => h.InteriorDesignId == design.Id).ToListAsync();
            db.InteriorDesignHighlights.RemoveRange(highlights);

            db.InteriorDesignSpecs.Add(new InteriorDesignSpec
            {
                InteriorDesignId = design.Id,
                Label = "Mới",
                Value = "50m2",
                SortOrder = 0,
            });

            await db.SaveChangesAsync();

            var logs = await db.SystemLogs.AsNoTracking().ToListAsync();

            Assert.DoesNotContain(logs, l =>
                l.Entity is nameof(InteriorDesignSpec)
                    or nameof(InteriorDesignHighlight)
                    or nameof(InteriorDesignProduct)
                    or nameof(InteriorDesignPackage)
                    or nameof(InteriorDesignMaterial)
                    or nameof(InteriorDesignImage));

            // children-only save → 1 log cha tổng hợp
            Assert.Contains(logs, l =>
                l.Entity == nameof(InteriorDesign)
                && l.Action == "UPDATE_INTERIOR_DESIGN");
            Assert.True(logs.Count <= 2, $"Expected ≤2 logs, got {logs.Count}: "
                + string.Join("; ", logs.Select(l => $"{l.Action}/{l.Entity}")));
        }
    }

    [Fact]
    public async Task Delete_interior_design_logs_parent_only()
    {
        var (db, _, sp) = TestDb.Create(withAudit: true);
        await using (sp)
        {
            var design = new InteriorDesign
            {
                Title = "To delete",
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
            };
            db.InteriorDesigns.Add(design);
            await db.SaveChangesAsync();
            db.InteriorDesignMaterials.Add(new InteriorDesignMaterial
            {
                InteriorDesignId = design.Id,
                Name = "Gỗ sồi",
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            db.SystemLogs.RemoveRange(db.SystemLogs);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var entity = await db.InteriorDesigns.FindAsync(design.Id);
            Assert.NotNull(entity);
            db.InteriorDesigns.Remove(entity!);
            await db.SaveChangesAsync();

            var logs = await db.SystemLogs.AsNoTracking().ToListAsync();
            Assert.Contains(logs, l =>
                l.Action == "DELETE_INTERIOR_DESIGN" && l.Entity == nameof(InteriorDesign));
            Assert.DoesNotContain(logs, l => l.Entity == nameof(InteriorDesignMaterial));
        }
    }
}
