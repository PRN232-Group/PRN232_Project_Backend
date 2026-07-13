namespace Com.FPTU.Prn232SE1918.Api.Application.Base;

public interface IBaseRepo<T> where T : class
{
    // Chỉ cho phép lấy ra một TẬP các entity T bất kỳ
    DbSet<T> Entities { get; }
    IApplicationDbContext ApplicationDbContext { get; } //Has-A
}
