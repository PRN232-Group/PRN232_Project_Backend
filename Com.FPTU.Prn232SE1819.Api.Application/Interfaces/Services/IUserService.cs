using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<IList<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(CreateUserDto dto, int actorId);
    Task<UserDto> UpdateRoleAsync(int id, string role, int actorId);
    Task<UserDto> SetLockedAsync(int id, bool isLocked, int actorId);
}
