namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IEmailSender
{
    /// <param name="htmlBody">Nội dung HTML (ưu tiên).</param>
    /// <param name="plainTextBody">Bản plain fallback cho client không đọc HTML.</param>
    Task SendAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);
}
