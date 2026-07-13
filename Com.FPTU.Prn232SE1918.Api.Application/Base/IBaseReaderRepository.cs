namespace Com.FPTU.Prn232SE1918.Api.Application.Base;
public interface IBaseReaderRepository<T>: IBaseRepo<T> where T : class
{
    /// <summary>
    /// Get all items of an entity by asynchronous (bat dong bo)
    /// </summary>
    /// <returns>List of T</returns>
    Task<IList<T>> GetAllAsync();

    T Find(params object[] keyValues);
    // T Find(object key);
    /// <summary>
    /// Lấy ra một thực thể T (an entity) theo keyId.
    /// </summary>
    /// <param name="keyValues"></param>
    /// <returns></returns>
    Task<T> FindAsync(params object[] keyValues);

}
