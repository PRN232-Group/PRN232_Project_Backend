namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class EmailOtp
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public string Otp { get; set; } = null!;
    public string? Payload { get; set; }
    public string? ResetToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
