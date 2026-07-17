namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class QuotationRequestProduct
{
    public int QuotationRequestId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public virtual QuotationRequest QuotationRequest { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
