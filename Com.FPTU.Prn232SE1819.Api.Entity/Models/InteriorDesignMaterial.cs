namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class InteriorDesignMaterial
{
    public int Id { get; set; }

    public int InteriorDesignId { get; set; }

    public string Name { get; set; } = null!;

    public string? Origin { get; set; }

    public string? Finish { get; set; }

    public string? Care { get; set; }

    public virtual InteriorDesign InteriorDesign { get; set; } = null!;
}
