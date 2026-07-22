using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public PermissionService(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<PermissionMatrixDto> GetMatrixAsync()
    {
        var roles = await _uow.Repository<Role>().Entities
            .Where(r => r.Name != "Production")
            .OrderBy(r => r.Id)
            .ToListAsync();

        var pages = await _uow.Repository<AppPage>().Entities
            .Where(p => p.IsActive)
            .OrderBy(p => p.Section)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.PageKey)
            .ToListAsync();

        var grants = await _uow.Repository<RolePermission>().Entities
            .Include(rp => rp.Page)
            .ToListAsync();

        var map = roles.ToDictionary(
            r => r.Id,
            r => grants
                .Where(g => g.RoleId == r.Id && g.Page.IsActive)
                .Select(g => g.Page.PageKey)
                .Distinct()
                .OrderBy(k => k)
                .ToList());

        return new PermissionMatrixDto
        {
            Roles = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
            }).ToList(),
            Pages = pages.Select(p => new AppPageDto
            {
                Id = p.Id,
                PageKey = p.PageKey,
                Name = p.Name,
                Section = p.Section,
                SortOrder = p.SortOrder,
            }).ToList(),
            Grants = map,
        };
    }

    public async Task SetRolePermissionsAsync(int roleId, SetRolePermissionsDto dto, int actorUserId)
    {
        var role = await _uow.Repository<Role>().FindAsync(roleId)
            ?? throw new KeyNotFoundException($"Role {roleId} not found.");

        if (role.Name == "Production")
            throw new InvalidOperationException("Không sửa quyền role Production.");
        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không được sửa quyền Admin — luôn full quyền.");
        if (string.Equals(role.Name, "Customer", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Customer không cấu hình trang back-office.");

        var keys = (dto.PageKeys ?? new List<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pages = await _uow.Repository<AppPage>().Entities
            .Where(p => p.IsActive && keys.Contains(p.PageKey))
            .ToListAsync();

        await _uow.BeginTransactionAsync();
        try
        {
            var existing = await _uow.Repository<RolePermission>().Entities
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            foreach (var row in existing)
                await _uow.Repository<RolePermission>().DeleteAsync(row, saveChanges: false);

            foreach (var page in pages)
            {
                await _uow.Repository<RolePermission>().InsertAsync(new RolePermission
                {
                    RoleId = roleId,
                    PageId = page.Id,
                }, saveChanges: false);
            }

            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        await _audit.LogAsync(
            action: "UPDATE_PERMISSIONS",
            entity: "RolePermission",
            entityId: roleId.ToString(),
            detail: $"Updated {pages.Count} permissions for role {role.Name}.",
            actorUserId: actorUserId);
    }

    public async Task<IReadOnlyList<string>> GetPageKeysForRoleAsync(int roleId)
    {
        return await _uow.Repository<RolePermission>().Entities
            .Where(rp => rp.RoleId == roleId && rp.Page.IsActive)
            .Select(rp => rp.Page.PageKey)
            .Distinct()
            .OrderBy(k => k)
            .ToListAsync();
    }
}
