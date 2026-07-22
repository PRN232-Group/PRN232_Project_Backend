using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface ISystemLogService
{
    Task<IList<SystemLogDto>> GetAllAsync(SystemLogQueryDto query);
}
