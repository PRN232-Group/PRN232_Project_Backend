using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Common;
using Microsoft.EntityFrameworkCore;
namespace Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;

public class ApplicationDbContext : IApplicationDbContext
{
    private DbFactoryContext _dbFactoryContext;
    public ApplicationDbContext(DbFactoryContext dbFactoryContext)
    {
        _dbFactoryContext = dbFactoryContext;
    }
    public DbContext DbContext => this._dbFactoryContext.DbContext;
}
