namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class InteriorDesignProduct
{
    public int InteriorDesignId { get; set; }

    public int ProductId { get; set; }

    public virtual InteriorDesign InteriorDesign { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
