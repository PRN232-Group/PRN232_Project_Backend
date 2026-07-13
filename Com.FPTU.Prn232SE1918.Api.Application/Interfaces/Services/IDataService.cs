using System.Linq.Expressions;

namespace Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Services;

public interface IDataService<TEntity> where TEntity : class, new()
{
    //CRUD: Create/Insert, Read/Retrive, Update, Delete
    Task<IList<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> GetOneAsync(int? id);
    Task UpdateAsync(TEntity entity);
    Task AddAsync(TEntity entity);
    Task DeleteAsync(int? id);
    Task DeleteAsync(TEntity entity);

}
