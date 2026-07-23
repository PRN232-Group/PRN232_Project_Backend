using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public UserService(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await FindWithRole(userId);
        return user == null || user.IsDeleted ? null : ToProfile(user);
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await FindWithRole(userId);
        if (user == null || user.IsDeleted) return null;

        var name = dto.FullName ?? dto.Name;
        if (!string.IsNullOrWhiteSpace(name)) user.FullName = name.Trim();
        if (dto.Phone != null) user.Phone = dto.Phone;
        if (dto.Address != null) user.Address = dto.Address;
        user.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<User>().UpdateAsync(user);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
        return ToProfile(user);
    }

    public async Task<IList<UserDto>> GetAllAsync()
    {
        var users = await _uow.Repository<User>().Entities
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .ToListAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, int actorId)
    {
        var actor = await FindWithRole(actorId)
            ?? throw new UnauthorizedAccessException("Actor not found.");
        if (!string.Equals(actor.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Only Admin can create users.");

        if (!RoleRanks.CanManage(actor.Role.Name, dto.Role))
            throw new InvalidOperationException("Cannot create user with equal or higher role.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _uow.Repository<User>().Entities.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists.");

        var role = await _uow.Repository<Role>().Entities
            .FirstOrDefaultAsync(r => r.Name == dto.Role)
            ?? throw new InvalidOperationException($"Role '{dto.Role}' not found.");

        var password = string.IsNullOrWhiteSpace(dto.Password) ? "ChangeMe@123" : dto.Password;
        var user = new User
        {
            Email = email,
            FullName = dto.Name.Trim(),
            Phone = dto.Phone,
            RoleId = role.Id,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            IsLocked = false,
            IsDeleted = false,
            CreatedAt = VnDateTime.Now,
        };

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<User>().InsertAsync(user);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        user.Role = role;
        await _audit.LogAsync(
            action: "CREATE_USER",
            entity: "User",
            entityId: user.Id.ToString(),
            detail: $"Created user {user.Email} with role {role.Name}.",
            actorUserId: actorId);
        return ToDto(user);
    }

    public async Task<UserDto> UpdateRoleAsync(int id, string role, int actorId)
    {
        var (actor, target) = await GetActorAndTarget(actorId, id);
        EnsureCanManage(actor, target);

        var nextRole = await _uow.Repository<Role>().Entities
            .FirstOrDefaultAsync(r => r.Name == role)
            ?? throw new InvalidOperationException($"Role '{role}' not found.");

        if (!RoleRanks.CanManage(actor.Role.Name, nextRole.Name))
            throw new InvalidOperationException("Cannot assign equal or higher role.");

        target.RoleId = nextRole.Id;
        target.Role = nextRole;
        target.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<User>().UpdateAsync(target);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        await _audit.LogAsync(
            action: "UPDATE_USER_ROLE",
            entity: "User",
            entityId: target.Id.ToString(),
            detail: $"Updated role to {nextRole.Name}.",
            actorUserId: actorId);
        return ToDto(target);
    }

    public async Task<UserDto> SetLockedAsync(int id, bool isLocked, int actorId)
    {
        var (actor, target) = await GetActorAndTarget(actorId, id);
        EnsureCanManage(actor, target);

        target.IsLocked = isLocked;
        target.UpdatedAt = VnDateTime.Now;

        await _uow.BeginTransactionAsync();
        try
        {
            await _uow.Repository<User>().UpdateAsync(target);
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }

        await _audit.LogAsync(
            action: "LOCK_USER",
            entity: "User",
            entityId: target.Id.ToString(),
            detail: isLocked ? "Locked user account." : "Unlocked user account.",
            actorUserId: actorId);
        return ToDto(target);
    }

    private async Task<(User actor, User target)> GetActorAndTarget(int actorId, int targetId)
    {
        var actor = await FindWithRole(actorId)
            ?? throw new UnauthorizedAccessException("Actor not found.");
        var target = await FindWithRole(targetId)
            ?? throw new KeyNotFoundException($"User {targetId} not found.");
        if (target.IsDeleted) throw new KeyNotFoundException($"User {targetId} not found.");
        return (actor, target);
    }

    private static void EnsureCanManage(User actor, User target)
    {
        if (actor.Id == target.Id)
            throw new InvalidOperationException("Cannot change yourself.");
        if (!RoleRanks.CanManage(actor.Role.Name, target.Role.Name))
            throw new InvalidOperationException("Cannot manage equal or higher role.");
    }

    private async Task<User?> FindWithRole(int id)
        => await _uow.Repository<User>().Entities
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Name = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role.Name,
        Status = u.IsLocked ? "Locked" : (u.IsActive ? "Active" : "Inactive"),
        IsLocked = u.IsLocked,
    };

    private static UserProfileDto ToProfile(User u) => new()
    {
        Id = u.Id,
        Name = u.FullName,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Address = u.Address,
        Role = u.Role.Name,
        AvatarUrl = u.AvatarUrl,
    };
}
