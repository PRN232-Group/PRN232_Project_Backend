using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Common;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    //1. Thuộc tính or field để lưu trữ instance của ApplicationDbContext
    //Cau truc se luu cac repository tuong ung voi moi entity (customers => CustomerRepo,...)
    private Dictionary<string, Object> Repositories { get; }
    private IDbContextTransaction _transaction;
    private IsolationLevel? _isolationLevel;

    /*
     * private set: chỉ có thể set giá trị cho ApplicationDbContext bên trong lớp UnitOfWork,
                    bên ngoài không thể set được
     */
    public IApplicationDbContext ApplicationDbContext{ get; private set; } 
    //2. Constructors
    public UnitOfWork(IApplicationDbContext applicationDbContext)
    {
        ApplicationDbContext = applicationDbContext;
        Repositories = new Dictionary<string, dynamic>();

    }

    public IRepository<T> Repository<T>() where T : class
    {
        //lam sao de return 1 repository cua mot entity T nao do?
        var type = typeof(T);//Muon lay kieu cua entity T
        var typeName = type.Name;
        //lock: giup tuan tu hoa neu nhu co nhieu loi de tao repo
        lock (Repositories)
        {
            if (Repositories.ContainsKey(typeName))
                return (IRepository<T>)Repositories[typeName];
            //tao moi repo tuong ung cho kieu T di vao
            var repository = new Repository<T>(ApplicationDbContext);
            //add vo cached 
            Repositories.Add(typeName, repository);

            return repository;
        }
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction == null)
        {
            // Thông báo cho EF Core rằng chúng ta muốn bắt đầu một transaction mới
            // với mức độ cô lập đã chỉ định (nếu có) --> Chưa được commit hoặc rollback
            _transaction = _isolationLevel.HasValue ?
                await ApplicationDbContext.DbContext.Database.BeginTransactionAsync(_isolationLevel.GetValueOrDefault()) :
                await ApplicationDbContext.DbContext.Database.BeginTransactionAsync();
        }
    }

    public async Task CommitTransactionAsync()
    {
        //apply (commit) toan bo transactions => database
        await ApplicationDbContext.DbContext.SaveChangesAsync(); 

        if (_transaction is null) return;
        //clean toan bo hanh dong da duoc apply vao db (xóa toàn bộ trên vùng nhớ tạm của EFC)
        await _transaction.CommitAsync();
        //nothing vung nho dem (neu co)
        await _transaction.DisposeAsync(); 
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null) return;
        //huy toan bo hanh dong da duoc apply vao db (xóa toàn bộ trên vùng nhớ tạm của EFC)
        await _transaction.RollbackAsync();
        //nothing vung nho dem (neu co) -- very importance
        await _transaction.DisposeAsync(); 
        _transaction = null;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await ApplicationDbContext.DbContext.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        //nothing: connection db, nothing all objects
        if (ApplicationDbContext == null) return;
        //close connection
        if (ApplicationDbContext.DbContext.Database.GetDbConnection().State == ConnectionState.Open)
            ApplicationDbContext.DbContext.Database.GetDbConnection().Close();

        ApplicationDbContext.DbContext.Dispose();

        ApplicationDbContext = null;

        GC.SuppressFinalize(this); //nothing luon UnitOfWork (khong con dung duoc nua)

    }

}
