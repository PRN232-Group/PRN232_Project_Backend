namespace Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    /// <summary>
    /// Application db context
    /// </summary>
    IApplicationDbContext ApplicationDbContext { get; }
    /// <summary>
    /// Get repository instance of an entity (T) inside Repository
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IRepository<T> Repository<T>() where T : class; //has-a: day la mot cai ham

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();

}
