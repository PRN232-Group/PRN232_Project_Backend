using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class SystemLogService : ISystemLogService, IAuditService
{
    private readonly IUnitOfWork _uow;

    public SystemLogService(IUnitOfWork uow) => _uow = uow;

    public async Task<SystemLogPageDto> GetAllAsync(SystemLogQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 50);

        var q = _uow.Repository<SystemLog>().Entities.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            q = q.Where(x => x.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            var entity = query.Entity.Trim();
            q = q.Where(x => x.Entity.Contains(entity));
        }

        if (query.From.HasValue)
            q = q.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(x => x.CreatedAt <= query.To.Value);

        var total = await q.CountAsync();

        var logs = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var actorIds = logs.Where(x => x.ActorUserId.HasValue)
            .Select(x => x.ActorUserId!.Value)
            .Distinct()
            .ToList();

        var actorNames = actorIds.Count == 0
            ? new Dictionary<int, string>()
            : await _uow.Repository<User>().Entities.AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName!);

        var items = logs.Select(x =>
        {
            string? actorName = null;
            if (x.ActorUserId is int actorId && actorNames.TryGetValue(actorId, out var name))
                actorName = name;
            return ToDto(x, actorName);
        }).ToList();

        return new SystemLogPageDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task LogAsync(string action, string entity, string? entityId, string? detail, int? actorUserId)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entity)) return;

        var log = new SystemLog
        {
            ActorUserId = actorUserId,
            Action = action.Trim(),
            Entity = entity.Trim(),
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim(),
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
            CreatedAt = VnDateTime.Now,
        };

        await _uow.Repository<SystemLog>().InsertAsync(log);
    }

    private static SystemLogDto ToDto(SystemLog x, string? actorName) => new()
    {
        Id = x.Id,
        ActorUserId = x.ActorUserId,
        ActorName = actorName,
        Action = x.Action,
        Entity = x.Entity,
        EntityId = x.EntityId,
        Detail = x.Detail,
        CreatedAt = x.CreatedAt,
    };
}
