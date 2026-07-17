namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class Quotation
{
    public int Id { get; set; }

    public int? QuotationRequestId { get; set; }

    public int CustomerId { get; set; }

    public string? Title { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public int? CreatedById { get; set; }

    public int? ApprovedById { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual QuotationRequest? QuotationRequest { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual User? CreatedBy { get; set; }

    public virtual User? ApprovedBy { get; set; }

    public virtual ICollection<QuotationProduct> QuotationProducts { get; set; } = new List<QuotationProduct>();
}
