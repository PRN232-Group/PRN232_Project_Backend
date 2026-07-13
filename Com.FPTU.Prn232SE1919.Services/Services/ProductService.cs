
using Com.FPTU.Prn232SE1819.Api.Caching;
using Com.FPTU.Prn232SE1819.Api.Caching.common;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Repositories;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;
using Com.FPTU.Prn232SE1919.Services.BaseServices;

using Com.FPTU.Prn232SE1819.Api.Caching.extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Com.FPTU.Prn232SE1919.Services.Services;

public class ProductService : DataServiceBase<Product>, IProductService
{
    private readonly IDataCached dataCached;

    // Cơ chế code theo kiểu Dependency Injection (DI) để inject các dependencies vào constructor của ProductService.

    public ProductService(IUnitOfWork unitOfWork, IDataCached dataCached) : base(unitOfWork)
    {
        this.dataCached = dataCached;
    }

    public override async Task AddAsync(Product entity)
    {
        string key = null;
        try
        {
            await UnitOfWork.BeginTransactionAsync();
            //luu vao database
            await UnitOfWork.Repository<Product>()
                    .InsertAsync(entity);
            //luu vao cache
            key = string.Format(CachingCommonDefaults.CacheKey, typeof(Product).Name,
               entity.ProductId).ToLower();

            dataCached.Set(key, entity, CachingCommonDefaults.CacheTime);


            await UnitOfWork.CommitTransactionAsync();

        }
        catch (Exception ex)
        {
            await UnitOfWork.RollbackTransactionAsync();
            dataCached.Remove(key);
            throw;
        }

    }

    public override Task DeleteAsync(int? id)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(Product entity)
    {
        throw new NotImplementedException();
    }

    public override async Task<IList<Product>> GetAllAsync()
    {
        string name = typeof(Product).Name.ToLower(); //"product"
        string keyAll = string.Format(CachingCommonDefaults.AllCacheKey, name);
        /*1. check xem toan bo du lieu Products duoc cached chua?*/
        if (!dataCached.IsSet(keyAll) || !(bool)dataCached.Get(keyAll))
        {
            /*load all product tu db => cached. after that set value true cho keyall*/
            await _loadAllProductToCached();
            dataCached.Set(keyAll, true, CachingCommonDefaults.CacheTime);
        }

        /*2. getall data tu cached ra ngoai*/
        string pattern = string.Format(CachingCommonDefaults.CacheKeyHeader, name);//solid.ecommerce.product
        var result = dataCached.GetValues<Product>(pattern);
        //sap xep tang dan theo productId truoc khi return
        result = result.OrderBy(p => p.ProductId)
            .ToList();

        return await Task.FromResult(result);

    }

    public override Task<IEnumerable<Product>> GetAllAsync(Expression<Func<Product, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public override async Task<Product> GetOneAsync(int? id)
    {
        try
        {
            string key = string.Format(CachingCommonDefaults.CacheKey, typeof(Product).Name.ToLower(), id);
            if (dataCached.IsSet(key))
                return await Task.FromResult(dataCached.Get<Product>(key));

            Product? p = await UnitOfWork.Repository<Product>()
                               .FindAsync(id!);
            if (p != null)
            {
                dataCached.Set(key, p, CachingCommonDefaults.CacheTime);
                return await Task.FromResult(p);
            }
            return null!;


        }
        catch (Exception ex)
        {
            return null!;
        }
    }

    public override Task UpdateAsync(Product entity)
    {
        throw new NotImplementedException();
    }

    private async Task _loadAllProductToCached()
    {
        /*goi tu db len*/
        var products = await UnitOfWork.Repository<Product>()
                             .Entities
                             .ToListAsync();

        string key = string.Empty;
        foreach (var p in products)
        {
            /*Lam sao sinh chuoi ky tu doi tuong p o tren?: fptu.ecommerce.product.1*/
            key = dataCached.GetKey(p, p => p.ProductId).ToLower();
            /*check xem key da duoc cached chua? neu chua thi set vao cache*/
            if (!dataCached.IsSet(key))
                dataCached.Set(key, p, CachingCommonDefaults.CacheTime);

        }
    }
}
