using System.Globalization;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>
/// Converts UTC values to Romanian local time and formats them for display as
/// <c>dd/MM/yyyy</c> and <c>dd/MM/yyyy, HH:mm</c> — day first, never the US month-first order.
///
/// Food expiry is a local calendar date, so <see cref="Today"/> is the reference used by every
/// expiry rule — never <c>DateTime.UtcNow.Date</c>, which is a different day for part of the night.
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

    /// <summary>Today's calendar date in Romania.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(ToLocal(DateTime.UtcNow));

    /// <summary>Today's calendar date in Romania, relative to a given instant (used by tests).</summary>
    public static DateOnly TodayAt(DateTime utcNow) => DateOnly.FromDateTime(ToLocal(utcNow));

    /// <summary>The earliest expiry date a donation published now may carry.</summary>
    public static DateOnly EarliestAllowedExpiry => Today.AddDays(FoodRules.MinimumShelfLifeDays);

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

    /// <summary>Day-first numeric date. Used for every date shown in the interface.</summary>
    public const string DateFormat = "dd/MM/yyyy";

    public const string DateTimeFormat = "dd/MM/yyyy, HH:mm";

    public const string TimeFormat = "HH:mm";

    /// <summary>What the user is told to type, e.g. in a placeholder.</summary>
    public const string DatePlaceholder = "zz/ll/aaaa";

    public const string TimePlaceholder = "hh:mm";

    public static string FormatDate(DateTime utc) => ToLocal(utc).ToString(DateFormat, Culture);

    public static string FormatDate(DateTime? utc) => utc.HasValue ? FormatDate(utc.Value) : "—";

    /// <summary>Formats a calendar date (e.g. a food expiry date) without any time-zone shift.</summary>
    public static string FormatDate(DateOnly date) => date.ToString(DateFormat, Culture);

    public static string FormatDateTime(DateTime utc) => ToLocal(utc).ToString(DateTimeFormat, Culture);

    public static string FormatDateTime(DateTime? utc) => utc.HasValue ? FormatDateTime(utc.Value) : "—";

    /// <summary>Long Romanian form, e.g. "15 septembrie 2026". Kept for prose contexts.</summary>
    public static string FormatDateLong(DateOnly date) => date.ToString("d MMMM yyyy", Culture);

    // ----- Form values -----
    // Date and time fields are plain text inputs in dd/MM/yyyy and HH:mm. A native
    // <input type="date"> renders in the *browser's* locale, which shows mm/dd/yyyy for anyone
    // on a US system and cannot be overridden from the page, so the format is handled here.

    /// <summary>Value for a date field: "22/09/2026", or empty.</summary>
    public static string ToDateInput(DateOnly? date) =>
        date.HasValue ? date.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : string.Empty;

    public static string ToDateInput(DateTime? utc) =>
        utc.HasValue ? ToLocal(utc.Value).ToString(DateFormat, CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>Value for a time field: "18:30", or empty.</summary>
    public static string ToTimeInput(DateTime? utc) =>
        utc.HasValue ? ToLocal(utc.Value).ToString(TimeFormat, CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>
    /// Parses a date the user typed. Accepts dd/MM/yyyy with "/", "." or "-" separators, and
    /// ISO yyyy-MM-dd so a pasted or browser-autofilled value still works.
    /// </summary>
    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "dd.MM.yyyy", "d.M.yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };

        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>Parses a 24-hour time the user typed ("18:30", "8:5", "18.30").</summary>
    public static TimeOnly? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats = { "HH:mm", "H:m", "HH.mm", "H.m", "HHmm" };

        return TimeOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// Human-friendly remaining shelf life, e.g. "expiră azi", "expiră mâine", "mai are 5 zile".
    /// </summary>
    public static string DescribeRemaining(DateOnly expiration)
    {
        var days = expiration.DayNumber - Today.DayNumber;
        return days switch
        {
            < 0 => "expirat",
            0 => "expiră azi",
            1 => "expiră mâine",
            _ => $"mai are {days} zile"
        };
    }
}
