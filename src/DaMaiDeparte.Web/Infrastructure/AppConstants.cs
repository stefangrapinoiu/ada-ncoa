namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>Application branding. Change the name here (and in wwwroot/manifest.webmanifest).</summary>
public static class AppInfo
{
    public const string Name = "Adă-ncoa";
    public const string ShortName = "Adă-ncoa";
    public const string Tagline = "Mai puțină risipă alimentară. Mai mult ajutor.";
    public const string CultureName = "ro-RO";
    public const string TimeZoneId = "Europe/Bucharest";
    public const string ThemeColor = "#047857";

    /// <summary>ISO code of the only country activated in Version 1.</summary>
    public const string DefaultCountryCode = "RO";
}

/// <summary>
/// Food-safety rules enforced server-side. There is a single user type and a single kind of
/// listing, so every rule here applies to every donation.
/// </summary>
public static class FoodRules
{
    /// <summary>
    /// A listing must still be valid for at least this many full calendar days when it is
    /// published: with a minimum of 3, food expiring today, tomorrow or the day after is rejected.
    /// </summary>
    public const int MinimumShelfLifeDays = 3;

    public const int MinImages = 1;

    public const int MaxImages = 3;

    /// <summary>Titles/descriptions longer than this are rejected before they reach the database.</summary>
    public const int MaxTitleLength = 100;

    public const int MaxPickupLocationLength = 200;

    public const int MaxPickupNotesLength = 500;
}

public static class SessionKeys
{
    public const string BrowsingLocation = "BrowsingLocation";
}

public static class TempDataKeys
{
    public const string Success = "StatusSuccess";
    public const string Error = "StatusError";
}
