using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IQuotationService
{
    Task<IList<QuotationDto>> GetAllAsync();
    Task<IList<QuotationDto>> GetMineAsync(int customerId);
    Task<QuotationDto> CreateAsync(int createdById, CreateQuotationDto dto);
    Task<QuotationDto> UpdateAsync(int id, int actorId, UpdateQuotationDto dto);
}
