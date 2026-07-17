using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IContentService
{
    Task<IList<ContentDto>> GetAllAsync(bool publishedOnly);
    Task<ContentDto?> GetByIdAsync(int id);
    Task<ContentDto?> GetBySlugAsync(string slug);
    Task<ContentDto> CreateAsync(ContentUpsertDto dto);
    Task<ContentDto> UpdateAsync(int id, ContentUpsertDto dto);
    Task DeleteAsync(int id);
}
