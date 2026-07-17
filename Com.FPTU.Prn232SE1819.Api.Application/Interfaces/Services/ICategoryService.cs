using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<IList<CategoryDto>> GetDtosAsync();
    Task<CategoryDto> CreateDtoAsync(CategoryUpsertDto dto);
    Task<CategoryDto> UpdateDtoAsync(int id, CategoryUpsertDto dto);
    Task DeleteAsync(int id);
}
