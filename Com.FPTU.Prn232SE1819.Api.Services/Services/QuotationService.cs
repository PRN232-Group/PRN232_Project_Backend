using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class QuotationService : IQuotationService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public QuotationService(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<IList<QuotationDto>> GetAllAsync()
    {
        var list = await Query().OrderByDescending(q => q.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<IList<QuotationDto>> GetMineAsync(int customerId)
    {
        var list = await Query()
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<QuotationDto> CreateAsync(int createdById, CreateQuotationDto dto)
    {
        QuotationRequest? req = null;
        if (dto.QuotationRequestId is int reqId)
        {
            req = await _uow.Repository<QuotationRequest>().Entities
                .Include(r => r.QuotationRequestProducts)
                .FirstOrDefaultAsync(r => r.Id == reqId)
                ?? throw new InvalidOperationException($"Yêu cầu báo giá #{reqId} không tồn tại.");
        }

        var customerId = dto.CustomerId ?? req?.CustomerId
            ?? throw new InvalidOperationException("Thiếu customerId.");

        var qtyByProduct = new Dictionary<int, int>();
        if (dto.Items != null)
        {
            foreach (var it in dto.Items)
            {
                if (it.ProductId <= 0) continue;
                var qty = it.Quantity <= 0 ? 1 : it.Quantity;
                qtyByProduct[it.ProductId] = qtyByProduct.TryGetValue(it.ProductId, out var prev)
                    ? prev + qty
                    : qty;
            }
        }
        if (req != null)
        {
            foreach (var rp in req.QuotationRequestProducts)
            {
                if (!qtyByProduct.ContainsKey(rp.ProductId))
                    qtyByProduct[rp.ProductId] = rp.Quantity <= 0 ? 1 : rp.Quantity;
            }
        }

        var productIds = (dto.ProductIds ?? new List<int>()).Distinct().Where(id => id > 0).ToList();
        if (productIds.Count == 0)
            productIds = qtyByProduct.Keys.ToList();
        foreach (var pid in productIds)
        {
            if (!qtyByProduct.ContainsKey(pid)) qtyByProduct[pid] = 1;
        }
        if (qtyByProduct.Count == 0)
            throw new InvalidOperationException("Chọn ít nhất một sản phẩm.");

        var products = await _uow.Repository<Product>().Entities
            .Where(p => qtyByProduct.Keys.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync();
        if (products.Count == 0)
            throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        var catalogSum = products.Sum(p => p.Price * qtyByProduct[p.Id]);
        var amount = dto.Amount > 0 ? dto.Amount : catalogSum;
        var notes = dto.Notes ?? dto.Note;

        var entity = new Quotation
        {
            QuotationRequestId = req?.Id,
            CustomerId = customerId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? req?.Title : dto.Title.Trim(),
            Amount = amount,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "PendingApproval" : dto.Status.Trim(),
            Notes = notes,
            CreatedById = createdById,
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Quotation>().InsertAsync(entity, saveChanges: false);
            await _uow.SaveChangesAsync();

            foreach (var p in products)
            {
                await _uow.Repository<QuotationProduct>().InsertAsync(
                    new QuotationProduct
                    {
                        QuotationId = entity.Id,
                        ProductId = p.Id,
                        Quantity = qtyByProduct[p.Id],
                        UnitPrice = p.Price,
                    },
                    saveChanges: false);
            }

            if (req != null)
            {
                req.Status = "Replied";
                if (string.IsNullOrWhiteSpace(req.Reply))
                    req.Reply = notes ?? $"Đã tạo báo giá #{entity.Id}";
                req.HandledById ??= createdById;
                req.UpdatedAt = VnDateTime.Now;
                await _uow.Repository<QuotationRequest>().UpdateAsync(req, saveChanges: false);
            }

            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        var loaded = await Query().FirstAsync(q => q.Id == entity.Id);
        return ToDto(loaded);
    }

    public async Task<QuotationDto> UpdateAsync(int id, int actorId, UpdateQuotationDto dto)
    {
        var entity = await Query().FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new KeyNotFoundException($"Quotation {id} not found.");

        var previousStatus = entity.Status;

        if (!string.IsNullOrWhiteSpace(dto.Title)) entity.Title = dto.Title.Trim();
        if (dto.Amount is decimal amt && amt > 0) entity.Amount = amt;
        var notes = dto.Notes ?? dto.Note;
        if (notes != null) entity.Notes = notes;

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = dto.Status.Trim();
            entity.Status = status;
            if (status is "Approved" or "Rejected")
                entity.ApprovedById = actorId;
        }

        entity.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Quotation>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        if (entity.Status == "Approved" && previousStatus != "Approved")
        {
            await _audit.LogAsync(
                action: "APPROVE_QUOTATION",
                entity: "Quotation",
                entityId: entity.Id.ToString(),
                detail: $"Approved quotation #{entity.Id}.",
                actorUserId: actorId);
        }

        return ToDto(entity);
    }

    private IQueryable<Quotation> Query() =>
        _uow.Repository<Quotation>().Entities
            .Include(q => q.Customer)
            .Include(q => q.QuotationProducts)
                .ThenInclude(qp => qp.Product)
                    .ThenInclude(p => p!.ProductSpec);

    private static QuotationDto ToDto(Quotation q)
    {
        var items = q.QuotationProducts.Select(qp =>
        {
            var p = qp.Product;
            var price = qp.UnitPrice ?? p?.Price ?? 0;
            return new QuotationLineDto
            {
                ProductId = qp.ProductId,
                ProductName = p?.Name ?? "",
                Quantity = qp.Quantity <= 0 ? 1 : qp.Quantity,
                Price = price,
                MarketPrice = p?.MarketPrice,
                Stock = p?.Stock ?? 0,
                ImageUrl = p?.ImageUrl,
                Specs = p?.ProductSpec == null ? null : new ProductSpecsDto
                {
                    Dimensions = p.ProductSpec.Dimensions,
                    Material = p.ProductSpec.Material,
                    Origin = p.ProductSpec.Origin,
                    Finish = p.ProductSpec.Finish,
                    WeightKg = p.ProductSpec.WeightKg,
                    WarrantyMonths = p.ProductSpec.WarrantyMonths,
                },
            };
        }).ToList();

        var catalogTotal = items.Sum(i => i.Price * i.Quantity);
        var marketTotal = items.Sum(i => (i.MarketPrice ?? i.Price) * i.Quantity);
        var total = q.Amount > 0 ? q.Amount : catalogTotal;

        return new QuotationDto
        {
            Id = q.Id,
            QuotationRequestId = q.QuotationRequestId,
            CustomerId = q.CustomerId,
            CustomerName = q.Customer?.FullName ?? "",
            CustomerEmail = q.Customer?.Email ?? "",
            Title = q.Title,
            Amount = q.Amount,
            TotalPrice = total,
            ProductIds = items.Select(i => i.ProductId).ToList(),
            Status = q.Status,
            Notes = q.Notes,
            Note = q.Notes,
            CreatedAt = q.CreatedAt,
            Items = items,
            CatalogTotal = catalogTotal,
            MarketTotal = marketTotal,
            Savings = Math.Max(0, marketTotal - total),
        };
    }
}
