namespace DaMaiDeparte.Web.Models;

/// <summary>
/// A country the platform operates in. Version 1 only activates Romania, but the
/// hierarchy (Country → City → Neighborhood) is data-driven so other countries can be
/// added later without changing the application.
/// </summary>
public class Country
{
    public int Id { get; set; }

    /// <summary>ISO 3166-1 alpha-2 code, e.g. "RO".</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<City> Cities { get; set; } = new List<City>();
}

public class City
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>URL/diacritic-free identifier, unique inside a country, e.g. "cluj-napoca".</summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Reserved for a future proximity search. Version 1 does not use coordinates at all
    // (see the "nearby = city + optional neighborhood" rule), but keeping the columns here
    // means adding kilometre-based filtering later does not require restructuring.
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public ICollection<Neighborhood> Neighborhoods { get; set; } = new List<Neighborhood>();
}

public class Neighborhood
{
    public int Id { get; set; }

    public int CityId { get; set; }

    public City City { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
