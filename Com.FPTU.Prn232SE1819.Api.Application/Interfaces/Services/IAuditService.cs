namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string? entityId, string? detail, int? actorUserId);
}
