using Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;
/// <summary>
/// Represents a factory context for managing database connections or operations.
/// </summary>
public class DbFactoryContext
{
    //fields
    private DbContext _dbContext;
    private Func<ProductDbContext> _instanceFunc;

    //properties
    public DbContext DbContext => this._dbContext ?? (this._dbContext = _instanceFunc.Invoke());
    
    //Constructor
    public DbFactoryContext(Func<ProductDbContext> dbContextFactory)
    {
        _instanceFunc = dbContextFactory;
    }
    //public DbContext GetDbContext()
    //{
    //    if (_dbContext == null)
    //    {
    //        _dbContext = _instanceFunc(); // Gọi factory method để tạo
    //    }
    //    return _dbContext;
    //}

}
