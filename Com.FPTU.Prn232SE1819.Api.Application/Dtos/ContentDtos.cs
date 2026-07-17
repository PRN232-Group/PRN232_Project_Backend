namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class ContentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Body { get; set; }
    public string? CoverUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class ContentUpsertDto
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Type { get; set; } = "Blog";
    public string? Body { get; set; }
    public string? CoverUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
}
