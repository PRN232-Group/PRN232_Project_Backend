using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Com.FPTU.Prn232SE1819.Api.Services.Common;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;

    public ReviewService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<ReviewDto>> GetByProductAsync(int productId)
    {
        var list = await _uow.Repository<ProductReview>().Entities
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ReviewDto> CreateAsync(int productId, int userId, CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new InvalidOperationException("Rating must be 1..5.");

        var productExists = await _uow.Repository<Product>().Entities
            .AnyAsync(p => p.Id == productId && !p.IsDeleted);
        if (!productExists) throw new KeyNotFoundException($"Product {productId} not found.");

        if (await _uow.Repository<ProductReview>().Entities
            .AnyAsync(r => r.ProductId == productId && r.UserId == userId))
            throw new InvalidOperationException("You already reviewed this product.");

        var entity = new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<ProductReview>().InsertAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        var user = await _uow.Repository<User>().FindAsync(userId);
        entity.User = user!;
        return ToDto(entity);
    }

    private static ReviewDto ToDto(ProductReview r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        UserId = r.UserId,
        UserName = r.User?.FullName,
        Rating = r.Rating,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt,
    };
}
