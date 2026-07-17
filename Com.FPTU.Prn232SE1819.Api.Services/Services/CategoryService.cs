using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Com.FPTU.Prn232SE1819.Api.Services.Common;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;

    public CategoryService(IUnitOfWork uow) => _uow = uow;

    public async Task<IList<CategoryDto>> GetDtosAsync()
    {
        var list = await _uow.Repository<Category>().Entities
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> CreateDtoAsync(CategoryUpsertDto dto)
    {
        var entity = new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = true,
            CreatedAt = VnDateTime.Now,
        };
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Category>().InsertAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return ToDto(entity);
    }

    public async Task<CategoryDto> UpdateDtoAsync(int id, CategoryUpsertDto dto)
    {
        var entity = await _uow.Repository<Category>().FindAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Category>().UpdateAsync(entity);
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
        var entity = await _uow.Repository<Category>().FindAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        entity.IsActive = false;
        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<Category>().UpdateAsync(entity);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
    };
}
