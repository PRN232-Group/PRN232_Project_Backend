namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class InteriorDesignPackage
{
    public int Id { get; set; }

    public int InteriorDesignId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Includes { get; set; }

    public virtual InteriorDesign InteriorDesign { get; set; } = null!;
}
