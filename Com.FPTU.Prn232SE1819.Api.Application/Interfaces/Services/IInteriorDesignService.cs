using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IInteriorDesignService
{
    Task<IList<InteriorDesignDetailDto>> GetAllAsync(bool includeUnpublished);
    Task<InteriorDesignDetailDto?> GetByIdAsync(int id, bool includeUnpublished);
    Task<InteriorDesignDetailDto> CreateAsync(InteriorDesignUpsertDto dto);
    Task<InteriorDesignDetailDto> UpdateAsync(int id, InteriorDesignUpsertDto dto);
    Task DeleteAsync(int id);
}
