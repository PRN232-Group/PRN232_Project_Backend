using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Com.FPTU.Prn232SE1819.Api.Services.Common;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;

    public ProductService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<ProductDto>> GetAllAsync()
    {
        var list = await Query().Where(p => !p.IsDeleted).OrderBy(p => p.Id).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await Query().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return p == null ? null : ToDto(p);
    }

    public async Task<IList<ProductDto>> SearchAsync(string? keyword)
    {
        var q = Query().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(p => p.Name.Contains(k) || (p.Description != null && p.Description.Contains(k)));
        }
        return (await q.OrderBy(p => p.Id).ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto)
    {
        var entity = FromUpsert(dto);
        entity.CreatedAt = VnDateTime.Now;
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Product>().InsertAsync(entity, saveChanges: false);
            await _uow.SaveChangesAsync();
            if (dto.Specs != null)
            {
                await _uow.Repository<ProductSpec>().InsertAsync(new ProductSpec
                {
                    ProductId = entity.Id,
                    Dimensions = dto.Specs.Dimensions,
                    Material = dto.Specs.Material,
                    Origin = dto.Specs.Origin,
                    Finish = dto.Specs.Finish,
                    WeightKg = dto.Specs.WeightKg,
                    WarrantyMonths = dto.Specs.WarrantyMonths,
                }, saveChanges: false);
            }
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductUpsertDto dto)
    {
        var entity = await Query().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"Product {id} not found.");

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        entity.Price = dto.Price;
        entity.MarketPrice = dto.MarketPrice;
        entity.Stock = dto.Stock;
        entity.CategoryId = dto.CategoryId;
        entity.ImageUrl = dto.ImageUrl;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Product>().UpdateAsync(entity, saveChanges: false);
            if (dto.Specs != null)
            {
                var spec = entity.ProductSpec;
                if (spec == null)
                {
                    await _uow.Repository<ProductSpec>().InsertAsync(new ProductSpec
                    {
                        ProductId = entity.Id,
                        Dimensions = dto.Specs.Dimensions,
                        Material = dto.Specs.Material,
                        Origin = dto.Specs.Origin,
                        Finish = dto.Specs.Finish,
                        WeightKg = dto.Specs.WeightKg,
                        WarrantyMonths = dto.Specs.WarrantyMonths,
                    }, saveChanges: false);
                }
                else
                {
                    spec.Dimensions = dto.Specs.Dimensions;
                    spec.Material = dto.Specs.Material;
                    spec.Origin = dto.Specs.Origin;
                    spec.Finish = dto.Specs.Finish;
                    spec.WeightKg = dto.Specs.WeightKg;
                    spec.WarrantyMonths = dto.Specs.WarrantyMonths;
                    await _uow.Repository<ProductSpec>().UpdateAsync(spec, saveChanges: false);
                }
            }
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return (await GetByIdAsync(id))!;
    }

    public async Task<ProductDto> UpdatePriceAsync(int id, decimal price)
    {
        if (price < 0) throw new InvalidOperationException("Price must be >= 0.");
        var entity = await _uow.Repository<Product>().FindAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");
        if (entity.IsDeleted) throw new KeyNotFoundException($"Product {id} not found.");
        entity.Price = price;
        entity.UpdatedAt = VnDateTime.Now;
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Product>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _uow.Repository<Product>().FindAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = VnDateTime.Now;
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Product>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    private IQueryable<Product> Query()
        => _uow.Repository<Product>().Entities
            .Include(p => p.Category)
            .Include(p => p.ProductSpec);

    private static Product FromUpsert(ProductUpsertDto dto) => new()
    {
        Name = dto.Name.Trim(),
        Description = dto.Description,
        Price = dto.Price,
        MarketPrice = dto.MarketPrice,
        Stock = dto.Stock,
        CategoryId = dto.CategoryId,
        ImageUrl = dto.ImageUrl,
        IsActive = dto.IsActive,
        IsDeleted = false,
    };

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        MarketPrice = p.MarketPrice,
        Stock = p.Stock,
        InStock = p.Stock > 0,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        ImageUrl = p.ImageUrl,
        IsActive = p.IsActive,
        Specs = p.ProductSpec == null ? null : new ProductSpecsDto
        {
            Dimensions = p.ProductSpec.Dimensions,
            Material = p.ProductSpec.Material,
            Origin = p.ProductSpec.Origin,
            Finish = p.ProductSpec.Finish,
            WeightKg = p.ProductSpec.WeightKg,
            WarrantyMonths = p.ProductSpec.WarrantyMonths,
        },
    };
}
