using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Services;

public interface ILocationService
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every active county, in every active country — small enough to cache in a page.</summary>
    Task<IReadOnlyList<County>> GetCountiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every active city, in every active county — small enough to cache in a page.</summary>
    Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Neighborhood>> GetNeighborhoodsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Neighborhood>> GetNeighborhoodsAsync(int cityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a country/county/city/neighborhood combination against the database and returns
    /// it resolved, or null when any part is missing, inactive or does not belong to its parent.
    /// County is an extra consistency check on top of country/city (already enough to resolve a
    /// location on its own) — pass null to skip it, e.g. when re-resolving a stored preference
    /// that never recorded a county.
    /// </summary>
    Task<BrowsingLocation?> ResolveAsync(
        int? countryId, int? countyId, int? cityId, int? neighborhoodId, CancellationToken cancellationToken = default);

    /// <summary>The location stored on the user's profile, if it is still valid.</summary>
    Task<BrowsingLocation?> ResolvePreferredAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Saves the supplied location as the user's preferred ("home") location.</summary>
    Task SavePreferredAsync(string userId, BrowsingLocation location, CancellationToken cancellationToken = default);
}

public sealed class LocationService : ILocationService
{
    private readonly ApplicationDbContext _db;

    public LocationService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default) =>
        await _db.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<County>> GetCountiesAsync(CancellationToken cancellationToken = default) =>
        await _db.Counties
            .AsNoTracking()
            .Where(c => c.IsActive && c.Country.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<City>> GetCitiesAsync(CancellationToken cancellationToken = default) =>
        await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsActive && c.Country.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Neighborhood>> GetNeighborhoodsAsync(CancellationToken cancellationToken = default) =>
        await _db.Neighborhoods
            .AsNoTracking()
            .Where(n => n.IsActive && n.City.IsActive)
            .OrderBy(n => n.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Neighborhood>> GetNeighborhoodsAsync(int cityId, CancellationToken cancellationToken = default) =>
        await _db.Neighborhoods
            .AsNoTracking()
            .Where(n => n.IsActive && n.CityId == cityId)
            .OrderBy(n => n.Name)
            .ToListAsync(cancellationToken);

    public async Task<BrowsingLocation?> ResolveAsync(
        int? countryId,
        int? countyId,
        int? cityId,
        int? neighborhoodId,
        CancellationToken cancellationToken = default)
    {
        if (countryId is null || cityId is null)
        {
            return null;
        }

        // One query: the city carries its country, so an invalid pair cannot resolve.
        var city = await _db.Cities
            .AsNoTracking()
            .Include(c => c.Country)
            .FirstOrDefaultAsync(
                c => c.Id == cityId.Value && c.CountryId == countryId.Value && c.IsActive && c.Country.IsActive,
                cancellationToken);

        if (city is null)
        {
            return null;
        }

        // County is an extra consistency check, not part of the identity: a caller that never
        // recorded one (an older stored preference) passes null and skips it.
        if (countyId.HasValue && city.CountyId != countyId.Value)
        {
            return null;
        }

        string? neighborhoodName = null;
        if (neighborhoodId is not null)
        {
            neighborhoodName = await _db.Neighborhoods
                .AsNoTracking()
                .Where(n => n.Id == neighborhoodId.Value && n.CityId == city.Id && n.IsActive)
                .Select(n => n.Name)
                .FirstOrDefaultAsync(cancellationToken);

            // A neighborhood from another city is dropped rather than silently widening the feed.
            if (neighborhoodName is null)
            {
                return null;
            }
        }

        return new BrowsingLocation(
            city.CountryId,
            city.Country.Name,
            city.Id,
            city.Name,
            neighborhoodName is null ? null : neighborhoodId,
            neighborhoodName);
    }

    public async Task<BrowsingLocation?> ResolvePreferredAsync(string userId, CancellationToken cancellationToken = default)
    {
        var preferred = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.PreferredCountryId,
                u.PreferredCityId,
                u.PreferredNeighborhoodId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (preferred is null)
        {
            return null;
        }

        // County isn't stored on the profile (it's derivable from the city), so it's not
        // re-checked here.
        var resolved = await ResolveAsync(
            preferred.PreferredCountryId,
            null,
            preferred.PreferredCityId,
            preferred.PreferredNeighborhoodId,
            cancellationToken);

        // The neighborhood may have been deactivated since; fall back to the city alone.
        return resolved ?? await ResolveAsync(preferred.PreferredCountryId, null, preferred.PreferredCityId, null, cancellationToken);
    }

    public async Task SavePreferredAsync(string userId, BrowsingLocation location, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.PreferredCountryId = location.CountryId;
        user.PreferredCityId = location.CityId;
        user.PreferredNeighborhoodId = location.NeighborhoodId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
