using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Common;
using Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
namespace Com.FPTU.Prn232SE1819.Api.Infrastructure.Repositories;

public sealed class Repository<T> : IRepository<T> where T : class
{
    public IApplicationDbContext ApplicationDbContext { get; private set;}

    // Chỉ bên trong lớp Repostory mới có thể set giá trị cho ApplicationDbContext, bên ngoài không thể set được
    // Dùng constructor để khởi tạo giá trị cho ApplicationDbContext
    public Repository(IApplicationDbContext applicationDbContext)
       => ApplicationDbContext = applicationDbContext;

    public DbSet<T> Entities 
        => ApplicationDbContext.DbContext.Set<T>(); // trả về một DbSet<T> để thao tác với bảng tương ứng trong cơ sở dữ liệu


    public async Task DeleteAsync(T entity, bool saveChanges = true)
    {
        try
        {
            Entities.Remove(entity); // đồng bộ
            if (saveChanges)
            {
                await ApplicationDbContext.DbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public async Task DeleteAsync(int id, bool saveChanges = true)
    {
        /*1. Lay ra entity T theo id*/
        var entity = await Entities.FindAsync(id);
        
        /*2. Xóa đi*/
        await DeleteAsync(entity);

        /*3. Để thực thi UoW*/
        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, bool saveChanges = true)
    {
        await DeleteRangeAsync(entities, saveChanges);
    }

    public T Find(params object[] keyValues)
        => Entities.Find(keyValues);

    public async Task<T> FindAsync(params object[] keyValues)
        => await Entities.FindAsync(keyValues).AsTask();


    public async Task<IList<T>> GetAllAsync()
        => await Entities.ToListAsync();

    public async Task InsertAsync(T entity, bool saveChanges = true)
    {
        await Entities.AddAsync(entity);
        //su dung den UnitOfWork => commit giao dich (insert vao db)
        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task InsertRangeAsync(IEnumerable<T> entities, bool saveChanges = true)
    {
        await ApplicationDbContext.DbContext.AddRangeAsync(entities);
        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(T entity, bool saveChanges = true)
    {
        Entities.Update(entity);//dong bo
        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, bool saveChanges = true)
    {
        Entities.UpdateRange(entities);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }
}
