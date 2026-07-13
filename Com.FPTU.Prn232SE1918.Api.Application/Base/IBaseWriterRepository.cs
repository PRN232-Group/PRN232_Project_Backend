
namespace Com.FPTU.Prn232SE1918.Api.Application.Base;
public interface IBaseWriterRepository<T> : IBaseRepo<T> where T : class
{
    Task InsertAsync(T entity, bool saveChanges = true);
    Task InsertRangeAsync(IEnumerable<T> entities, bool saveChanges = true);
    Task UpdateAsync(T entity, bool saveChanges = true);
    Task UpdateRangeAsync(IEnumerable<T> entities, bool saveChanges = true);
    Task DeleteAsync(T entity, bool saveChanges = true);
    /*Overloading method*/
    Task DeleteAsync(int id, bool saveChanges = true);
    Task DeleteRangeAsync(IEnumerable<T> entities, bool saveChanges = true);
}
