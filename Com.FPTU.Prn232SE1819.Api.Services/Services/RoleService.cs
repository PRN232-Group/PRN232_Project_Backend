using Com.FPTU.Prn232SE1819.Api.Caching;
using Com.FPTU.Prn232SE1819.Api.Caching.common;
using Com.FPTU.Prn232SE1819.Api.Caching.extensions;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.BaseServices;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class RoleService : DataServiceBase<Role>, IRoleService
{
    private readonly IDataCached _dataCached;

    public RoleService(IUnitOfWork unitOfWork, IDataCached dataCached) : base(unitOfWork)
    {
        _dataCached = dataCached;
    }

    public override async Task AddAsync(Role entity)
    {
        string key = null!;
        try
        {
            await UnitOfWork.BeginTransactionAsync();
            await UnitOfWork.Repository<Role>().InsertAsync(entity);
            key = string.Format(CachingCommonDefaults.CacheKey, typeof(Role).Name, entity.Id).ToLower();
            _dataCached.Set(key, entity, CachingCommonDefaults.CacheTime);
            InvalidateAll();
            await UnitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync();
            if (key != null) _dataCached.Remove(key);
            throw;
        }
    }

    public override async Task DeleteAsync(int? id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        try
        {
            await UnitOfWork.BeginTransactionAsync();
            var entity = await UnitOfWork.Repository<Role>().FindAsync(id);
            if (entity == null) throw new KeyNotFoundException($"Role {id} not found.");
            await UnitOfWork.Repository<Role>().DeleteAsync(entity);
            _dataCached.Remove(string.Format(CachingCommonDefaults.CacheKey, typeof(Role).Name, id).ToLower());
            InvalidateAll();
            await UnitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public override async Task DeleteAsync(Role entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        await DeleteAsync(entity.Id);
    }

    public override async Task<IList<Role>> GetAllAsync()
    {
        string name = typeof(Role).Name.ToLower();
        string keyAll = string.Format(CachingCommonDefaults.AllCacheKey, name);
        if (!_dataCached.IsSet(keyAll) || !(bool)_dataCached.Get(keyAll))
        {
            await LoadAllToCache();
            _dataCached.Set(keyAll, true, CachingCommonDefaults.CacheTime);
        }
        string pattern = string.Format(CachingCommonDefaults.CacheKeyHeader, name);
        return _dataCached.GetValues<Role>(pattern).OrderBy(r => r.Id).ToList();
    }

    public override async Task<IEnumerable<Role>> GetAllAsync(Expression<Func<Role, bool>> predicate)
        => await UnitOfWork.Repository<Role>().Entities.Where(predicate).ToListAsync();

    public override async Task<Role> GetOneAsync(int? id)
    {
        string key = string.Format(CachingCommonDefaults.CacheKey, typeof(Role).Name.ToLower(), id);
        if (_dataCached.IsSet(key))
            return _dataCached.Get<Role>(key);

        var role = await UnitOfWork.Repository<Role>().FindAsync(id!);
        if (role != null)
            _dataCached.Set(key, role, CachingCommonDefaults.CacheTime);
        return role!;
    }

    public override async Task UpdateAsync(Role entity)
    {
        try
        {
            await UnitOfWork.BeginTransactionAsync();
            await UnitOfWork.Repository<Role>().UpdateAsync(entity);
            string key = string.Format(CachingCommonDefaults.CacheKey, typeof(Role).Name, entity.Id).ToLower();
            _dataCached.Set(key, entity, CachingCommonDefaults.CacheTime);
            InvalidateAll();
            await UnitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task LoadAllToCache()
    {
        var roles = await UnitOfWork.Repository<Role>().Entities.ToListAsync();
        foreach (var r in roles)
        {
            string key = _dataCached.GetKey(r, x => x.Id).ToLower();
            if (!_dataCached.IsSet(key))
                _dataCached.Set(key, r, CachingCommonDefaults.CacheTime);
        }
    }

    private void InvalidateAll()
    {
        string keyAll = string.Format(CachingCommonDefaults.AllCacheKey, typeof(Role).Name.ToLower());
        _dataCached.Remove(keyAll);
    }
}
