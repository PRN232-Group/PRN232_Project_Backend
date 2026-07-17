namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class DesignPriceCompareDto
{
    public decimal Studio { get; set; }
    public decimal MarketAvg { get; set; }
}

public class DesignSpecItemDto
{
    public string Label { get; set; } = null!;
    public string Value { get; set; } = null!;
}

public class DesignMaterialItemDto
{
    public string Name { get; set; } = null!;
    public string? Origin { get; set; }
    public string? Finish { get; set; }
    public string? Care { get; set; }
}

public class DesignPackageItemDto
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Includes { get; set; }
}

public class InteriorDesignUpsertDto
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Style { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? Gallery { get; set; }
    public string? Description { get; set; }
    public decimal? AreaSqm { get; set; }
    public decimal? BudgetFrom { get; set; }
    public decimal? BudgetTo { get; set; }
    public int? TimelineWeeks { get; set; }
    public DesignPriceCompareDto? PriceCompare { get; set; }
    public List<int>? RelatedProductIds { get; set; }
    public List<string>? Highlights { get; set; }
    public List<DesignSpecItemDto>? Specs { get; set; }
    public List<DesignMaterialItemDto>? Materials { get; set; }
    public List<DesignPackageItemDto>? Packages { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class InteriorDesignDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Style { get; set; }
    public string? ImageUrl { get; set; }
    public string? Image { get; set; }
    public List<string> Gallery { get; set; } = new();
    public string? Description { get; set; }
    public decimal? AreaSqm { get; set; }
    public decimal? BudgetFrom { get; set; }
    public decimal? BudgetTo { get; set; }
    public int? TimelineWeeks { get; set; }
    public DesignPriceCompareDto PriceCompare { get; set; } = new();
    public List<int> RelatedProductIds { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public List<DesignSpecItemDto> Specs { get; set; } = new();
    public List<DesignMaterialItemDto> Materials { get; set; } = new();
    public List<DesignPackageItemDto> Packages { get; set; } = new();
    public bool IsPublished { get; set; }
    public List<ProductDto> RelatedProducts { get; set; } = new();
}
