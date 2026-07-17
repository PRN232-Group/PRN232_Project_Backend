namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class QuotationProduct
{
    public int QuotationId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal? UnitPrice { get; set; }

    public virtual Quotation Quotation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
