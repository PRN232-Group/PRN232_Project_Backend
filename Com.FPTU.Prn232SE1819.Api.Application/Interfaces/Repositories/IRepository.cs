namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Repositories;

public interface IRepository<T> : 
    IBaseReaderRepository<T>,
    IBaseWriterRepository<T>,
    IBaseRepo<T> where T : class
{

}
