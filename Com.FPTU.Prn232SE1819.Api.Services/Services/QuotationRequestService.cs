using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class QuotationRequestService : IQuotationRequestService
{
    private readonly IUnitOfWork _uow;

    public QuotationRequestService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<QuotationRequestDto>> GetAllAsync()
    {
        var list = await Query().OrderByDescending(r => r.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<IList<QuotationRequestDto>> GetMineAsync(int customerId)
    {
        var list = await Query()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<QuotationRequestDto> CreateAsync(int customerId, CreateQuotationRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Nhập tiêu đề yêu cầu.");

        var lines = NormalizeItems(dto);
        if (lines.Count == 0)
            throw new InvalidOperationException("Chọn ít nhất một sản phẩm.");

        var productIds = lines.Select(x => x.ProductId).ToList();
        var products = await _uow.Repository<Product>().Entities
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted && p.IsActive)
            .ToListAsync();
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("Một số sản phẩm không còn bán.");

        _ = await _uow.Repository<User>().FindAsync(customerId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var entity = new QuotationRequest
        {
            CustomerId = customerId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Status = "Pending",
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<QuotationRequest>().InsertAsync(entity, saveChanges: false);
            await _uow.SaveChangesAsync();

            foreach (var line in lines)
            {
                await _uow.Repository<QuotationRequestProduct>().InsertAsync(
                    new QuotationRequestProduct
                    {
                        QuotationRequestId = entity.Id,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                    },
                    saveChanges: false);
            }
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        var loaded = await Query().FirstAsync(r => r.Id == entity.Id);
        return ToDto(loaded);
    }

    public async Task<QuotationRequestDto> ReplyAsync(int id, int handledById, ReplyQuotationRequestDto dto)
    {
        var entity = await Query().FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"QuotationRequest {id} not found.");

        var note = dto.ReplyNote ?? dto.Reply ?? dto.Message;
        entity.Reply = note;
        entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Replied" : dto.Status.Trim();
        entity.HandledById = handledById;
        entity.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<QuotationRequest>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        return ToDto(entity);
    }

    public async Task<QuotationRequestDto> UpdateAsync(int id, UpdateQuotationRequestDto dto)
    {
        var entity = await Query().FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"QuotationRequest {id} not found.");

        if (!string.IsNullOrWhiteSpace(dto.Title)) entity.Title = dto.Title.Trim();
        if (dto.Description != null) entity.Description = dto.Description;
        if (!string.IsNullOrWhiteSpace(dto.Status)) entity.Status = dto.Status.Trim();
        var note = dto.ReplyNote ?? dto.Reply;
        if (note != null) entity.Reply = note;
        entity.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<QuotationRequest>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        return ToDto(entity);
    }

    private static List<(int ProductId, int Quantity)> NormalizeItems(CreateQuotationRequestDto dto)
    {
        var map = new Dictionary<int, int>();
        foreach (var it in dto.Items ?? new List<CreateQuotationRequestItemDto>())
        {
            if (it.ProductId <= 0) continue;
            var qty = it.Quantity <= 0 ? 1 : it.Quantity;
            map[it.ProductId] = map.TryGetValue(it.ProductId, out var prev) ? prev + qty : qty;
        }
        foreach (var pid in dto.ProductIds ?? new List<int>())
        {
            if (pid <= 0) continue;
            if (!map.ContainsKey(pid)) map[pid] = 1;
        }
        return map.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    private IQueryable<QuotationRequest> Query() =>
        _uow.Repository<QuotationRequest>().Entities
            .Include(r => r.Customer)
            .Include(r => r.QuotationRequestProducts)
                .ThenInclude(rp => rp.Product)
                    .ThenInclude(p => p!.ProductSpec);

    private static QuotationRequestDto ToDto(QuotationRequest r)
    {
        var lines = r.QuotationRequestProducts
            .Where(rp => rp.Product != null)
            .Select(rp =>
            {
                var p = rp.Product!;
                var qty = rp.Quantity <= 0 ? 1 : rp.Quantity;
                return new QuotationRequestLineDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    MarketPrice = p.MarketPrice,
                    Stock = p.Stock,
                    Quantity = qty,
                    ImageUrl = p.ImageUrl,
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
            })
            .ToList();

        return new QuotationRequestDto
        {
            Id = r.Id,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.FullName ?? "",
            Title = r.Title,
            Description = r.Description,
            ProductIds = lines.Select(l => l.ProductId).ToList(),
            Status = r.Status,
            Reply = r.Reply,
            ReplyNote = r.Reply,
            CreatedAt = r.CreatedAt,
            Lines = lines,
            Products = lines.Select(l => new ProductDto
            {
                Id = l.ProductId,
                Name = l.Name,
                Description = l.Description,
                Price = l.Price,
                MarketPrice = l.MarketPrice,
                Stock = l.Stock,
                InStock = l.Stock > 0,
                ImageUrl = l.ImageUrl,
                IsActive = true,
                Specs = l.Specs,
            }).ToList(),
            EstimateTotal = lines.Sum(l => l.Price * l.Quantity),
            MarketTotal = lines.Sum(l => (l.MarketPrice ?? l.Price) * l.Quantity),
        };
    }
}
