using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        var host = _config["Smtp:Host"];
        var user = _config["Smtp:User"];
        var password = _config["Smtp:Password"];
        var from = _config["Smtp:From"] ?? user;
        var fromName = _config["Smtp:FromName"] ?? "Interior Studio";
        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var enableSsl = !string.Equals(_config["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
        var logAlways = !string.Equals(_config["Smtp:LogOtpToConsole"], "false", StringComparison.OrdinalIgnoreCase);

        var plain = plainTextBody ?? StripRoughHtml(htmlBody);

        if (logAlways)
            _logger.LogWarning("EMAIL → {To} | {Subject} | {Plain}", toEmail, subject, plain);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP chưa cấu hình (Smtp:Password). OTP đã log console để test.");
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(user, password),
        };

        using var msg = new MailMessage
        {
            From = new MailAddress(from!, fromName),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
        };
        msg.To.Add(toEmail);

        var plainView = AlternateView.CreateAlternateViewFromString(
            plain, Encoding.UTF8, MediaTypeNames.Text.Plain);
        var htmlView = AlternateView.CreateAlternateViewFromString(
            htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
        msg.AlternateViews.Add(plainView);
        msg.AlternateViews.Add(htmlView);

        await client.SendMailAsync(msg);
    }

    private static string StripRoughHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var t = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        t = System.Net.WebUtility.HtmlDecode(t);
        return System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim();
    }
}
