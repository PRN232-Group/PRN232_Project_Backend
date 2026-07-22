using System;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class SystemLog
{
    public long Id { get; set; }

    public int? ActorUserId { get; set; }

    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }
}