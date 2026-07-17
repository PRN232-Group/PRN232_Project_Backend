using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IOrderService
{
    Task<IList<OrderDto>> GetMineAsync(int userId);
    Task<IList<OrderDto>> GetAllAsync();
    Task<OrderDto?> GetByIdAsync(int id, int? requesterId, string? requesterRole);
    Task<OrderDto> CheckoutAsync(int userId, CheckoutRequestDto dto);
    Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
}
