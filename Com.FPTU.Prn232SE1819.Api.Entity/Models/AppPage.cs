namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class AppPage
{
    public int Id { get; set; }
    public string PageKey { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Section { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
