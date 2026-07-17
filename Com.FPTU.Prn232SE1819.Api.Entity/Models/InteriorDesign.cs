using System;
using System.Collections.Generic;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class InteriorDesign
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Category { get; set; }

    public string? Style { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public decimal? AreaSqm { get; set; }

    public decimal? BudgetFrom { get; set; }

    public decimal? BudgetTo { get; set; }

    public int? TimelineWeeks { get; set; }

    public decimal? StudioPrice { get; set; }

    public decimal? MarketAvgPrice { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<InteriorDesignImage> InteriorDesignImages { get; set; } = new List<InteriorDesignImage>();

    public virtual ICollection<InteriorDesignHighlight> InteriorDesignHighlights { get; set; } = new List<InteriorDesignHighlight>();

    public virtual ICollection<InteriorDesignSpec> InteriorDesignSpecs { get; set; } = new List<InteriorDesignSpec>();

    public virtual ICollection<InteriorDesignMaterial> InteriorDesignMaterials { get; set; } = new List<InteriorDesignMaterial>();

    public virtual ICollection<InteriorDesignPackage> InteriorDesignPackages { get; set; } = new List<InteriorDesignPackage>();

    public virtual ICollection<InteriorDesignProduct> InteriorDesignProducts { get; set; } = new List<InteriorDesignProduct>();
}
