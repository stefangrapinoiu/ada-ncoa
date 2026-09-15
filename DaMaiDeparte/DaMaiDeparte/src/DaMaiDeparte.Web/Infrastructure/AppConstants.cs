namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>Application branding. Change the name here (and in wwwroot/manifest.webmanifest).</summary>
public static class AppInfo
{
    public const string Name = "Dă Mai Departe";
    public const string ShortName = "Dă Mai Departe";
    public const string Tagline = "Dă lucrurilor o nouă viață.";
    public const string CultureName = "ro-RO";
    public const string TimeZoneId = "Europe/Bucharest";
    public const string ThemeColor = "#198754";
}

public static class AppRoles
{
    public const string Donator = "Donator";
    public const string Receiver = "Receiver";

    public static readonly string[] All = { Donator, Receiver };
}

public static class AppPolicies
{
    public const string DonatorOnly = "DonatorOnly";
    public const string ReceiverOnly = "ReceiverOnly";
}

public static class TempDataKeys
{
    public const string Success = "StatusSuccess";
    public const string Error = "StatusError";
}
