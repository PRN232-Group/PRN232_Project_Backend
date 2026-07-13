using System;
using System.Collections.Generic;

namespace Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    public string ReviewerName { get; set; } = null!;

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? ReviewDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
