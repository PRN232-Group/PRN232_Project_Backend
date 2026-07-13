using System;
using System.Collections.Generic;

namespace Com.FPTU.Prn232SE1918.MssqlServer.Entity.Models;

public partial class Recommendation
{
    public int RecommendationId { get; set; }

    public int ProductId { get; set; }

    public string UserName { get; set; } = null!;

    public bool IsRecommended { get; set; }

    public DateTime? RecommendDate { get; set; }

    public virtual Product Product { get; set; } = null!;
}
