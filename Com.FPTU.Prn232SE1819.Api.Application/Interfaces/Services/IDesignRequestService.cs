using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IDesignRequestService
{
    Task<DesignRequestDto> CreateAsync(int customerId, CreateDesignRequestDto dto);
    Task<List<DesignRequestDto>> GetAllAsync();
    Task<List<DesignRequestDto>> GetMineAsync(int customerId);
    Task<DesignRequestDto?> GetByIdAsync(int id, int currentUserId, string role);
    Task<DesignRequestDto> UpdateStatusAsync(int id, string newStatus, int? actorUserId = null);
}