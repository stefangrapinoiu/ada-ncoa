using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>
/// Backs the shared _LocationFields partial, which renders the Țara / Orașul / Cartierul
/// selects used on registration, on the location chooser and on the donation form.
///
/// All active cities and neighborhoods are rendered into the page and filtered client-side.
/// The data set is small (tens of rows), which avoids an AJAX endpoint and keeps the form
/// usable without JavaScript — the server re-validates the parent/child relationship anyway.
/// </summary>
public sealed class LocationPickerModel
{
    public required IReadOnlyList<Country> Countries { get; init; }

    public required IReadOnlyList<City> Cities { get; init; }

    public required IReadOnlyList<Neighborhood> Neighborhoods { get; init; }

    public int? CountryId { get; init; }

    public int? CityId { get; init; }

    public int? NeighborhoodId { get; init; }

    /// <summary>Form field names, so the same partial works with differently named models.</summary>
    public string CountryField { get; init; } = "CountryId";

    public string CityField { get; init; } = "CityId";

    public string NeighborhoodField { get; init; } = "NeighborhoodId";

    /// <summary>Text of the empty neighborhood option, e.g. "Toate cartierele" on a filter.</summary>
    public string NeighborhoodEmptyLabel { get; init; } = Resources.UiText.Location.NoNeighborhood;

    public bool ShowNeighborhoodHelp { get; init; } = true;

    /// <summary>Renders three columns on wide screens instead of stacking.</summary>
    public bool Inline { get; init; } = true;

    /// <summary>Element id for a field name such as "Input.CityId".</summary>
    public static string IdFor(string fieldName) => fieldName.Replace('.', '_');
}
