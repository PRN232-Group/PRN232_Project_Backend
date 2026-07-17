namespace Com.FPTU.Prn232SE1819.Api.Services.Common;

/// <summary>Giờ Việt Nam (UTC+7) cho CreatedAt / UpdatedAt khi ghi DB.</summary>
public static class VnDateTime
{
    private static readonly TimeZoneInfo Tz = ResolveTz();

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);

    private static TimeZoneInfo ResolveTz()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "VN", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
        }
    }
}
