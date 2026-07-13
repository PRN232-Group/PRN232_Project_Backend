using System;
using System.Collections.Generic;

namespace Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
