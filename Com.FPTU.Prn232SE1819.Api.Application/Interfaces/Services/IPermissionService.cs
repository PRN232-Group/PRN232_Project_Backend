using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IPermissionService
{
    Task<PermissionMatrixDto> GetMatrixAsync();
    Task SetRolePermissionsAsync(int roleId, SetRolePermissionsDto dto, int actorUserId);
    Task<IReadOnlyList<string>> GetPageKeysForRoleAsync(int roleId);
}
