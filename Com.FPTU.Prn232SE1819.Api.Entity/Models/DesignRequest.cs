using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

[Table("DesignRequests")]
public class DesignRequest
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Style { get; set; }

    public int? InteriorDesignId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Budget { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "New";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual User? Customer { get; set; }

    [ForeignKey(nameof(InteriorDesignId))]
    public virtual InteriorDesign? InteriorDesign { get; set; }

    public virtual ICollection<DesignRequestProduct> Products { get; set; } = new List<DesignRequestProduct>();
    public virtual ICollection<DesignRequestAttachment> Attachments { get; set; } = new List<DesignRequestAttachment>();
}

[Table("DesignRequestProducts")]
public class DesignRequestProduct
{
    public int DesignRequestId { get; set; }
    public int ProductId { get; set; }

    [ForeignKey(nameof(DesignRequestId))]
    public virtual DesignRequest? DesignRequest { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }
}

[Table("DesignRequestAttachments")]
public class DesignRequestAttachment
{
    [Key]
    public int Id { get; set; }

    public int DesignRequestId { get; set; }

    [Required]
    [Column("Url")]
    public string FileUrl { get; set; } = string.Empty;

    [ForeignKey(nameof(DesignRequestId))]
    public virtual DesignRequest? DesignRequest { get; set; }
}