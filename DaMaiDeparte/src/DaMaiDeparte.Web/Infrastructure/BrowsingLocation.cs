using System.Text.Json;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>
/// The location the user is currently browsing. This is deliberately separate from the
/// preferred ("home") location stored on the profile: someone who lives in Cluj-Napoca can
/// browse București for an afternoon without their account changing.
/// </summary>
public sealed record BrowsingLocation(
    int CountryId,
    string CountryName,
    int CityId,
    string CityName,
    int? NeighborhoodId,
    string? NeighborhoodName)
{
    /// <summary>"Cluj-Napoca" or "Cluj-Napoca · Mărăști".</summary>
    public string Display => NeighborhoodName is null ? CityName : $"{CityName} · {NeighborhoodName}";
}

public interface IBrowsingLocationStore
{
    BrowsingLocation? Get();

    void Set(BrowsingLocation location);

    void Clear();
}

/// <summary>
/// Stores the active browsing location in the session. A cookie-backed session is enough for
/// the MVP: losing it only means the user is asked "Unde vrei să cauți?" again.
/// </summary>
public sealed class SessionBrowsingLocationStore : IBrowsingLocationStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor _accessor;

    public SessionBrowsingLocationStore(IHttpContextAccessor accessor) => _accessor = accessor;

    public BrowsingLocation? Get()
    {
        var session = _accessor.HttpContext?.Session;
        var payload = session?.GetString(SessionKeys.BrowsingLocation);
        if (string.IsNullOrEmpty(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BrowsingLocation>(payload, Json);
        }
        catch (JsonException)
        {
            // A stale or tampered value should never break the request.
            session!.Remove(SessionKeys.BrowsingLocation);
            return null;
        }
    }

    public void Set(BrowsingLocation location) =>
        _accessor.HttpContext?.Session.SetString(SessionKeys.BrowsingLocation, JsonSerializer.Serialize(location, Json));

    public void Clear() => _accessor.HttpContext?.Session.Remove(SessionKeys.BrowsingLocation);
}
