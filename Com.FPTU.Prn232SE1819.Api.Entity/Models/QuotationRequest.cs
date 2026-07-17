namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class QuotationRequest
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = "Pending";

    public string? Reply { get; set; }

    public int? HandledById { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual User? HandledBy { get; set; }

    public virtual ICollection<QuotationRequestProduct> QuotationRequestProducts { get; set; } = new List<QuotationRequestProduct>();

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}
