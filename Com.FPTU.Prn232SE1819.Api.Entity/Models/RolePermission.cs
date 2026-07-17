namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class RolePermission
{
    public int RoleId { get; set; }
    public int PageId { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual AppPage Page { get; set; } = null!;
}
