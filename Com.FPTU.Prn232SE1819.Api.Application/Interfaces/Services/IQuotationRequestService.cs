using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IQuotationRequestService
{
    Task<IList<QuotationRequestDto>> GetAllAsync();
    Task<IList<QuotationRequestDto>> GetMineAsync(int customerId);
    Task<QuotationRequestDto> CreateAsync(int customerId, CreateQuotationRequestDto dto);
    Task<QuotationRequestDto> ReplyAsync(int id, int handledById, ReplyQuotationRequestDto dto);
    Task<QuotationRequestDto> UpdateAsync(int id, UpdateQuotationRequestDto dto);
}
