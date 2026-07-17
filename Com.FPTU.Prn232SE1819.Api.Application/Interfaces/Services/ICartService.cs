using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface ICartService
{
    Task<IList<CartItemDto>> GetAsync(int userId);
    Task<CartItemDto> AddAsync(int userId, AddCartItemDto dto);
    Task<CartItemDto> UpdateAsync(int userId, int cartItemId, UpdateCartItemDto dto);
    Task RemoveAsync(int userId, int cartItemId);
}
