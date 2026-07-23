using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Infrastructure.Context;

public class ApplicationDbContext : IApplicationDbContext
{
    private readonly DbFactoryContext _dbFactoryContext;

    public ApplicationDbContext(DbFactoryContext dbFactoryContext)
    {
        _dbFactoryContext = dbFactoryContext;
    }

    public DbContext DbContext => _dbFactoryContext.DbContext;


    public DbSet<T> Set<T>() where T : class
    {
        return _dbFactoryContext.DbContext.Set<T>();
    }


    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbFactoryContext.DbContext
            .SaveChangesAsync(cancellationToken);
    }
}