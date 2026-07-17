namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

/// <summary>HTML email theo brand FE (clay / cream / Playfair + Poppins).</summary>
public static class EmailTemplates
{
    public static (string Html, string Plain) OtpRegister(string otp, int minutes = 10)
        => BuildOtp(
            eyebrow: "Xác minh đăng ký",
            title: "Hoàn tất tài khoản của bạn",
            lead: "Cảm ơn bạn đã chọn Interior Studio. Nhập mã bên dưới để kích hoạt tài khoản.",
            otp: otp,
            minutes: minutes,
            plainSubjectHint: "đăng ký");

    public static (string Html, string Plain) OtpForgotPassword(string otp, int minutes = 10)
        => BuildOtp(
            eyebrow: "Đặt lại mật khẩu",
            title: "Mã xác minh của bạn",
            lead: "Chúng tôi nhận được yêu cầu đặt lại mật khẩu. Dùng mã bên dưới để tiếp tục.",
            otp: otp,
            minutes: minutes,
            plainSubjectHint: "đặt lại mật khẩu");

    private static (string Html, string Plain) BuildOtp(
        string eyebrow,
        string title,
        string lead,
        string otp,
        int minutes,
        string plainSubjectHint)
    {
        var year = DateTime.UtcNow.Year;
        var digits = string.Join("", otp.Select(c =>
            $"<td style=\"padding:0 4px;\"><div style=\"width:42px;height:52px;line-height:52px;text-align:center;font-family:Georgia,'Times New Roman',serif;font-size:26px;font-weight:700;letter-spacing:1px;color:#2c2723;background:#f0e6da;border:1px solid #ddc0a1;border-radius:10px;\">{c}</div></td>"));

        var html = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
  <title>Interior Studio</title>
  <!--[if mso]><style>body,table,td{{font-family:Georgia,serif!important}}</style><![endif]-->
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@600;700&family=Poppins:wght@400;500;600&display=swap');
    @media only screen and (max-width:620px) {{
      .wrapper {{ padding: 16px 10px !important; }}
      .card {{ border-radius: 18px !important; }}
      .pad {{ padding: 28px 20px !important; }}
      .brand {{ font-size: 22px !important; }}
      .title {{ font-size: 24px !important; }}
      .otp-cell div {{ width: 36px !important; height: 46px !important; line-height: 46px !important; font-size: 22px !important; }}
    }}
  </style>
</head>
<body style=""margin:0;padding:0;background:#f7f4ef;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;"">
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;"">Mã OTP Interior Studio: {otp} — hiệu lực {minutes} phút</div>
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f7f4ef;"">
    <tr>
      <td class=""wrapper"" align=""center"" style=""padding:36px 16px;"">
        <table role=""presentation"" class=""card"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:560px;background:#ffffff;border-radius:28px;overflow:hidden;box-shadow:0 24px 60px rgba(44,39,35,0.12);border:1px solid #e7ded2;"">
          <tr>
            <td style=""background:linear-gradient(135deg,#b0784f 0%,#8a5b34 100%);padding:28px 32px;"">
              <div class=""brand"" style=""font-family:'Playfair Display',Georgia,'Times New Roman',serif;font-size:26px;font-weight:700;color:#ffffff;letter-spacing:0.3px;"">Interior Studio</div>
              <div style=""margin-top:6px;font-family:'Poppins',system-ui,-apple-system,Segoe UI,sans-serif;font-size:12px;font-weight:500;letter-spacing:0.12em;text-transform:uppercase;color:rgba(255,255,255,0.82);"">{eyebrow}</div>
            </td>
          </tr>
          <tr>
            <td class=""pad"" style=""padding:36px 32px 28px;background:#ffffff;"">
              <h1 class=""title"" style=""margin:0 0 12px;font-family:'Playfair Display',Georgia,'Times New Roman',serif;font-size:28px;font-weight:600;line-height:1.25;color:#2c2723;"">{title}</h1>
              <p style=""margin:0 0 28px;font-family:'Poppins',system-ui,-apple-system,Segoe UI,sans-serif;font-size:15px;line-height:1.65;color:#4a423b;"">{lead}</p>
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" align=""center"" style=""margin:0 auto 8px;"">
                <tr class=""otp-cell"">{digits}</tr>
              </table>
              <p style=""margin:20px 0 0;text-align:center;font-family:'Poppins',system-ui,-apple-system,Segoe UI,sans-serif;font-size:13px;color:#8a7f74;"">Mã có hiệu lực <strong style=""color:#8a5b34;"">{minutes} phút</strong></p>
              <div style=""margin:28px 0 0;padding:14px 16px;background:#f0e6da;border-radius:12px;border:1px solid #ddc0a1;"">
                <p style=""margin:0;font-family:'Poppins',system-ui,-apple-system,Segoe UI,sans-serif;font-size:13px;line-height:1.55;color:#4a423b;"">
                  Vì bảo mật, <strong>không chia sẻ</strong> mã này với bất kỳ ai. Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.
                </p>
              </div>
            </td>
          </tr>
          <tr>
            <td style=""padding:20px 32px 28px;background:#efe9e1;border-top:1px solid #e7ded2;"">
              <p style=""margin:0;font-family:'Poppins',system-ui,-apple-system,Segoe UI,sans-serif;font-size:12px;line-height:1.5;color:#8a7f74;text-align:center;"">
                © {year} Interior Studio. Không gian sống được thiết kế tinh tế.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        var plain =
            $"Interior Studio — OTP {plainSubjectHint}\n\n" +
            $"Mã OTP của bạn: {otp}\n" +
            $"Hiệu lực: {minutes} phút.\n" +
            "Không chia sẻ mã này với bất kỳ ai.\n\n" +
            "Nếu bạn không yêu cầu, hãy bỏ qua email này.";

        return (html, plain);
    }
}
