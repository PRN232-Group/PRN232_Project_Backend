using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class ContentService : IContentService
{
    private readonly IUnitOfWork _uow;

    public ContentService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<ContentDto>> GetAllAsync(bool publishedOnly)
    {
        var q = _uow.Repository<Content>().Entities.AsQueryable();
        if (publishedOnly)
            q = q.Where(c => c.IsPublished);
        var list = await q
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ContentDto?> GetByIdAsync(int id)
    {
        var c = await _uow.Repository<Content>().FindAsync(id);
        return c == null ? null : ToDto(c);
    }

    public async Task<ContentDto?> GetBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var key = slug.Trim().ToLowerInvariant();
        var c = await _uow.Repository<Content>().Entities
            .FirstOrDefaultAsync(x => x.Slug.ToLower() == key && x.IsPublished);
        return c == null ? null : ToDto(c);
    }

    public async Task<ContentDto> CreateAsync(ContentUpsertDto dto)
    {
        Validate(dto);
        await EnsureUniqueSlugAsync(dto.Slug.Trim(), excludeId: null);

        var entity = new Content
        {
            Title = dto.Title.Trim(),
            Slug = dto.Slug.Trim(),
            Type = string.IsNullOrWhiteSpace(dto.Type) ? "Blog" : dto.Type.Trim(),
            Body = dto.Body,
            CoverUrl = dto.CoverUrl,
            IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? (dto.PublishedAt ?? VnDateTime.Now) : null,
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Content>().InsertAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return ToDto(entity);
    }

    public async Task<ContentDto> UpdateAsync(int id, ContentUpsertDto dto)
    {
        Validate(dto);
        var entity = await _uow.Repository<Content>().FindAsync(id)
            ?? throw new KeyNotFoundException($"Content {id} not found.");

        await EnsureUniqueSlugAsync(dto.Slug.Trim(), excludeId: id);

        entity.Title = dto.Title.Trim();
        entity.Slug = dto.Slug.Trim();
        entity.Type = string.IsNullOrWhiteSpace(dto.Type) ? "Blog" : dto.Type.Trim();
        entity.Body = dto.Body;
        entity.CoverUrl = dto.CoverUrl;
        entity.IsPublished = dto.IsPublished;
        entity.UpdatedAt = VnDateTime.Now;
        if (dto.IsPublished)
            entity.PublishedAt = dto.PublishedAt ?? entity.PublishedAt ?? VnDateTime.Now;
        else
            entity.PublishedAt = null;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Content>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _uow.Repository<Content>().FindAsync(id)
            ?? throw new KeyNotFoundException($"Content {id} not found.");
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Content>().DeleteAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task EnsureUniqueSlugAsync(string slug, int? excludeId)
    {
        var exists = await _uow.Repository<Content>().Entities
            .AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value));
        if (exists)
            throw new InvalidOperationException("Slug đã tồn tại.");
    }

    private static void Validate(ContentUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidOperationException("Title is required.");
        if (string.IsNullOrWhiteSpace(dto.Slug))
            throw new InvalidOperationException("Slug is required.");
    }

    private static ContentDto ToDto(Content c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Slug = c.Slug,
        Type = c.Type,
        Body = c.Body,
        CoverUrl = c.CoverUrl,
        IsPublished = c.IsPublished,
        PublishedAt = c.PublishedAt,
    };
}
