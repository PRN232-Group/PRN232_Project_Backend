using System.ComponentModel.DataAnnotations;

namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class CreateDesignRequestDto
{
    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    public string Title { get; set; } = string.Empty;

    public string? Style { get; set; }
    public int? InteriorDesignId { get; set; }
    public List<int>? RelatedProductIds { get; set; }
    public decimal? Budget { get; set; }
    public string? Notes { get; set; }
    public List<string>? Attachments { get; set; }
}

public class UpdateDesignRequestStatusDto
{
    [Required(ErrorMessage = "Trạng thái không được để trống.")]
    public string Status { get; set; } = string.Empty;
}

public class DesignRequestDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Style { get; set; }
    public int? InteriorDesignId { get; set; }
    public decimal? Budget { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<int> RelatedProductIds { get; set; } = new();
    public List<string> Attachments { get; set; } = new();
}