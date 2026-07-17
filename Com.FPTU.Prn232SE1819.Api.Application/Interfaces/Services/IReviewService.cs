using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IReviewService
{
    Task<IList<ReviewDto>> GetByProductAsync(int productId);
    Task<ReviewDto> CreateAsync(int productId, int userId, CreateReviewDto dto);
}
