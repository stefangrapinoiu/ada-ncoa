using System.Globalization;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>
/// Converts UTC values to Romanian local time and formats them for display,
/// e.g. "15 septembrie 2026" or "15 septembrie 2026, 18:30".
/// </summary>
public static class RoDate
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo(AppInfo.CultureName);

    private static readonly Lazy<TimeZoneInfo> Zone = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(AppInfo.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Windows without ICU time zone mapping.
            return TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
        }
    });

    public static TimeZoneInfo TimeZone => Zone.Value;

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);

    /// <summary>Interprets a value entered by the user (Romanian local time) and converts it to UTC.</summary>
    public static DateTime LocalToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (TimeZone.IsInvalidTime(unspecified))
        {
            // Skipped hour at DST start: move forward one hour.
            unspecified = unspecified.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, TimeZone);
    }

    public static string FormatDate(DateTime utc) => ToLocal(utc).ToString("d MMMM yyyy", Culture);

    public static string FormatDate(DateTime? utc) => utc.HasValue ? FormatDate(utc.Value) : "—";

    public static string FormatDateTime(DateTime utc) => ToLocal(utc).ToString("d MMMM yyyy, HH:mm", Culture);

    public static string FormatDateTime(DateTime? utc) => utc.HasValue ? FormatDateTime(utc.Value) : "—";

    /// <summary>Value for an &lt;input type="datetime-local"&gt;.</summary>
    public static string ToInputValue(DateTime? utc) =>
        utc.HasValue ? ToLocal(utc.Value).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture) : string.Empty;
}
