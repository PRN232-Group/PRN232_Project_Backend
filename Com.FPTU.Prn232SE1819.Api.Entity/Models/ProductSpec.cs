using System;
using System.Collections.Generic;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class ProductSpec
{
    public int ProductId { get; set; }

    public string? Dimensions { get; set; }

    public string? Material { get; set; }

    public string? Origin { get; set; }

    public string? Finish { get; set; }

    public decimal? WeightKg { get; set; }

    public int? WarrantyMonths { get; set; }

    public virtual Product Product { get; set; } = null!;
}
