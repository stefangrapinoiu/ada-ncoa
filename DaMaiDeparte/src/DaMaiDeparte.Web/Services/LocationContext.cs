using DaMaiDeparte.Web.Infrastructure;

namespace DaMaiDeparte.Web.Services;

public interface ILocationContext
{
    /// <summary>
    /// The location currently being browsed. Reads the session first; if nothing is stored
    /// (new session, expired cookie), it falls back to the user's preferred location and
    /// remembers it for the rest of the session. Returns null when the user has never chosen
    /// a location, in which case the caller sends them to /Location.
    /// </summary>
    Task<BrowsingLocation?> GetAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Reads the session only, without touching the database.</summary>
    BrowsingLocation? Peek();

    void Set(BrowsingLocation location);

    void Clear();
}

public sealed class LocationContext : ILocationContext
{
    private readonly IBrowsingLocationStore _store;
    private readonly ILocationService _locations;

    public LocationContext(IBrowsingLocationStore store, ILocationService locations)
    {
        _store = store;
        _locations = locations;
    }

    public async Task<BrowsingLocation?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var current = _store.Get();
        if (current is not null)
        {
            return current;
        }

        var preferred = await _locations.ResolvePreferredAsync(userId, cancellationToken);
        if (preferred is not null)
        {
            _store.Set(preferred);
        }

        return preferred;
    }

    public BrowsingLocation? Peek() => _store.Get();

    public void Set(BrowsingLocation location) => _store.Set(location);

    public void Clear() => _store.Clear();
}
