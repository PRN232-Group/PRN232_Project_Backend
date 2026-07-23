namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class SystemLogDto
{
    public long Id { get; set; }
    public int? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = null!;
    public string Entity { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SystemLogQueryDto
{
    public string? Action { get; set; }
    public string? Entity { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
