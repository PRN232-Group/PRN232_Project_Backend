using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class InteriorDesignService : IInteriorDesignService
{
    private readonly IUnitOfWork _uow;

    public InteriorDesignService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<InteriorDesignDetailDto>> GetAllAsync(bool includeUnpublished)
    {
        var q = Query();
        if (!includeUnpublished)
            q = q.Where(d => d.IsPublished);

        var list = await q.OrderBy(d => d.Id).ToListAsync();
        return list.Select(d => ToDto(d, includeRelatedProducts: false)).ToList();
    }

    public async Task<InteriorDesignDetailDto?> GetByIdAsync(int id, bool includeUnpublished)
    {
        var entity = await Query().FirstOrDefaultAsync(d => d.Id == id);
        if (entity == null) return null;
        if (!includeUnpublished && !entity.IsPublished) return null;
        return ToDto(entity, includeRelatedProducts: true);
    }

    public async Task<InteriorDesignDetailDto> CreateAsync(InteriorDesignUpsertDto dto)
    {
        Validate(dto);
        var entity = new InteriorDesign
        {
            Title = dto.Title.Trim(),
            CreatedAt = VnDateTime.Now,
        };
        ApplyRoot(entity, dto);

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<InteriorDesign>().InsertAsync(entity, saveChanges: false);
            await _uow.SaveChangesAsync(); // cần Id trước khi gắn bảng con
            await ReplaceChildrenAsync(entity.Id, dto, saveNow: false);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        return (await GetByIdAsync(entity.Id, includeUnpublished: true))!;
    }

    public async Task<InteriorDesignDetailDto> UpdateAsync(int id, InteriorDesignUpsertDto dto)
    {
        Validate(dto);
        var entity = await Query().FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"InteriorDesign {id} not found.");

        ApplyRoot(entity, dto);
        entity.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            // Chỉ đánh dấu root — tránh Entities.Update() kéo cả graph con → log/cascade nhiễu
            _uow.Repository<InteriorDesign>().Entities.Entry(entity).State = EntityState.Modified;
            await ReplaceChildrenAsync(id, dto, saveNow: false);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        return (await GetByIdAsync(id, includeUnpublished: true))!;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _uow.Repository<InteriorDesign>().FindAsync(id)
            ?? throw new KeyNotFoundException($"InteriorDesign {id} not found.");

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<InteriorDesign>().DeleteAsync(entity, saveChanges: false);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    private IQueryable<InteriorDesign> Query() =>
        _uow.Repository<InteriorDesign>().Entities
            .Include(d => d.InteriorDesignImages)
            .Include(d => d.InteriorDesignHighlights)
            .Include(d => d.InteriorDesignSpecs)
            .Include(d => d.InteriorDesignMaterials)
            .Include(d => d.InteriorDesignPackages)
            .Include(d => d.InteriorDesignProducts)
                .ThenInclude(lp => lp.Product)
                    .ThenInclude(p => p!.Category);

    private static void Validate(InteriorDesignUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Title is required.");
    }

    private static void ApplyRoot(InteriorDesign entity, InteriorDesignUpsertDto dto)
    {
        entity.Title = dto.Title.Trim();
        entity.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category.Trim();
        entity.Style = string.IsNullOrWhiteSpace(dto.Style) ? null : dto.Style.Trim();
        entity.Description = dto.Description;
        entity.AreaSqm = dto.AreaSqm;
        entity.BudgetFrom = dto.BudgetFrom;
        entity.BudgetTo = dto.BudgetTo;
        entity.TimelineWeeks = dto.TimelineWeeks;
        entity.StudioPrice = dto.PriceCompare?.Studio;
        entity.MarketAvgPrice = dto.PriceCompare?.MarketAvg;
        entity.IsPublished = dto.IsPublished;

        var gallery = (dto.Gallery ?? new List<string>())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        entity.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl)
            ? gallery.FirstOrDefault()
            : dto.ImageUrl.Trim();
    }

    private async Task ReplaceChildrenAsync(int designId, InteriorDesignUpsertDto dto, bool saveNow = true)
    {
        var images = await _uow.Repository<InteriorDesignImage>().Entities
            .Where(x => x.InteriorDesignId == designId).ToListAsync();
        foreach (var row in images)
            await _uow.Repository<InteriorDesignImage>().DeleteAsync(row, saveChanges: false);

        var highlights = await _uow.Repository<InteriorDesignHighlight>().Entities
            .Where(x => x.InteriorDesignId == designId).ToListAsync();
        foreach (var row in highlights)
            await _uow.Repository<InteriorDesignHighlight>().DeleteAsync(row, saveChanges: false);

        var specs = await _uow.Repository<InteriorDesignSpec>().Entities
            .Where(x => x.InteriorDesignId == designId).ToListAsync();
        foreach (var row in specs)
            await _uow.Repository<InteriorDesignSpec>().DeleteAsync(row, saveChanges: false);

        var materials = await _uow.Repository<InteriorDesignMaterial>().Entities
            .Where(x => x.InteriorDesignId == designId).ToListAsync();
        foreach (var row in materials)
            await _uow.Repository<InteriorDesignMaterial>().DeleteAsync(row, saveChanges: false);

        var packages = await _uow.Repository<InteriorDesignPackage>().Entities
            .Where(x => x.InteriorDesignId == designId).ToListAsync();
        foreach (var row in packages)
            await _uow.Repository<InteriorDesignPackage>().DeleteAsync(row, saveChanges: false);

        var links = await _uow.Repository<InteriorDesignProduct>().Entities
            .Where(x => x.InteriorDesignId == designId).ToListAsync();
        foreach (var row in links)
            await _uow.Repository<InteriorDesignProduct>().DeleteAsync(row, saveChanges: false);

        var gallery = (dto.Gallery ?? new List<string>())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (gallery.Count == 0 && !string.IsNullOrWhiteSpace(dto.ImageUrl))
            gallery.Add(dto.ImageUrl.Trim());

        for (var i = 0; i < gallery.Count; i++)
        {
            await _uow.Repository<InteriorDesignImage>().InsertAsync(new InteriorDesignImage
            {
                InteriorDesignId = designId,
                Url = gallery[i],
                SortOrder = i,
            }, saveChanges: false);
        }

        var hl = (dto.Highlights ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToList();
        for (var i = 0; i < hl.Count; i++)
        {
            await _uow.Repository<InteriorDesignHighlight>().InsertAsync(new InteriorDesignHighlight
            {
                InteriorDesignId = designId,
                Text = hl[i],
                SortOrder = i,
            }, saveChanges: false);
        }

        var specItems = (dto.Specs ?? new List<DesignSpecItemDto>())
            .Where(s => !string.IsNullOrWhiteSpace(s.Label))
            .ToList();
        for (var i = 0; i < specItems.Count; i++)
        {
            await _uow.Repository<InteriorDesignSpec>().InsertAsync(new InteriorDesignSpec
            {
                InteriorDesignId = designId,
                Label = specItems[i].Label.Trim(),
                Value = (specItems[i].Value ?? string.Empty).Trim(),
                SortOrder = i,
            }, saveChanges: false);
        }

        foreach (var m in dto.Materials ?? new List<DesignMaterialItemDto>())
        {
            if (string.IsNullOrWhiteSpace(m.Name)) continue;
            await _uow.Repository<InteriorDesignMaterial>().InsertAsync(new InteriorDesignMaterial
            {
                InteriorDesignId = designId,
                Name = m.Name.Trim(),
                Origin = m.Origin,
                Finish = m.Finish,
                Care = m.Care,
            }, saveChanges: false);
        }

        foreach (var p in dto.Packages ?? new List<DesignPackageItemDto>())
        {
            if (string.IsNullOrWhiteSpace(p.Name)) continue;
            await _uow.Repository<InteriorDesignPackage>().InsertAsync(new InteriorDesignPackage
            {
                InteriorDesignId = designId,
                Name = p.Name.Trim(),
                Price = p.Price,
                Includes = p.Includes,
            }, saveChanges: false);
        }

        var productIds = (dto.RelatedProductIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (productIds.Count > 0)
        {
            var valid = await _uow.Repository<Product>().Entities
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();
            foreach (var pid in valid)
            {
                await _uow.Repository<InteriorDesignProduct>().InsertAsync(new InteriorDesignProduct
                {
                    InteriorDesignId = designId,
                    ProductId = pid,
                }, saveChanges: false);
            }
        }

        if (saveNow)
            await _uow.SaveChangesAsync();
    }

    private static InteriorDesignDetailDto ToDto(InteriorDesign d, bool includeRelatedProducts)
    {
        var gallery = d.InteriorDesignImages
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Url)
            .ToList();
        if (gallery.Count == 0 && !string.IsNullOrWhiteSpace(d.ImageUrl))
            gallery.Add(d.ImageUrl);

        var relatedIds = d.InteriorDesignProducts.Select(x => x.ProductId).Distinct().ToList();
        var dto = new InteriorDesignDetailDto
        {
            Id = d.Id,
            Title = d.Title,
            Category = d.Category,
            Style = d.Style,
            ImageUrl = d.ImageUrl,
            Image = d.ImageUrl,
            Gallery = gallery,
            Description = d.Description,
            AreaSqm = d.AreaSqm,
            BudgetFrom = d.BudgetFrom,
            BudgetTo = d.BudgetTo,
            TimelineWeeks = d.TimelineWeeks,
            PriceCompare = new DesignPriceCompareDto
            {
                Studio = d.StudioPrice ?? 0,
                MarketAvg = d.MarketAvgPrice ?? 0,
            },
            RelatedProductIds = relatedIds,
            Highlights = d.InteriorDesignHighlights
                .OrderBy(x => x.SortOrder)
                .Select(x => x.Text)
                .ToList(),
            Specs = d.InteriorDesignSpecs
                .OrderBy(x => x.SortOrder)
                .Select(x => new DesignSpecItemDto { Label = x.Label, Value = x.Value })
                .ToList(),
            Materials = d.InteriorDesignMaterials
                .Select(x => new DesignMaterialItemDto
                {
                    Name = x.Name,
                    Origin = x.Origin,
                    Finish = x.Finish,
                    Care = x.Care,
                })
                .ToList(),
            Packages = d.InteriorDesignPackages
                .Select(x => new DesignPackageItemDto
                {
                    Name = x.Name,
                    Price = x.Price,
                    Includes = x.Includes,
                })
                .ToList(),
            IsPublished = d.IsPublished,
        };

        if (includeRelatedProducts)
        {
            dto.RelatedProducts = d.InteriorDesignProducts
                .Select(x => x.Product)
                .Where(p => p != null && !p.IsDeleted)
                .DistinctBy(p => p!.Id)
                .Select(p => new ProductDto
                {
                    Id = p!.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    MarketPrice = p.MarketPrice,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive,
                })
                .ToList();
            // mark in-stock for related cards
            foreach (var rp in dto.RelatedProducts)
                rp.InStock = rp.Stock > 0;
        }

        return dto;
    }
}
