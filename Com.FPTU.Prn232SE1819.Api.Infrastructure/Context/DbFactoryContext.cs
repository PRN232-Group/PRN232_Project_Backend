using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;

public class DbFactoryContext
{
    private DbContext _dbContext;
    private Func<InteriorStudioDbContext> _instanceFunc;

    public DbContext DbContext => this._dbContext ?? (this._dbContext = _instanceFunc.Invoke());

    public DbFactoryContext(Func<InteriorStudioDbContext> dbContextFactory)
    {
        _instanceFunc = dbContextFactory;
    }
}
