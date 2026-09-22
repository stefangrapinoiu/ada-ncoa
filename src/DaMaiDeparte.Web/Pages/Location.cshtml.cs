using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages;

/// <summary>
/// "Locația mea" — the location setting. It is chosen once at registration and changed here,
/// so this page is never an interstitial: login goes straight to the dashboard. Saving writes
/// the profile and refreshes the session, so there is a single location to reason about.
/// </summary>
public class LocationModel : PageModel
{
    private readonly ILocationService _locations;
    private readonly ILocationContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LocationModel(ILocationService locations, ILocationContext context, UserManager<ApplicationUser> userManager)
    {
        _locations = locations;
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public int? CountryId { get; set; }

    [BindProperty]
    public int? CountyId { get; set; }

    [BindProperty]
    public int? CityId { get; set; }

    [BindProperty]
    public int? NeighborhoodId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public LocationPickerModel Picker { get; private set; } = null!;

    /// <summary>True when the user already has a location, so "Anulează" makes sense.</summary>
    public bool HasCurrentLocation { get; private set; }

    /// <summary>Shown when the account has no usable location yet and the feed cannot be built.</summary>
    public bool IsFirstTimeSetup { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        // Preselect the user's last used or preferred values.
        var current = await _context.GetAsync(userId, cancellationToken);
        HasCurrentLocation = current is not null;
        IsFirstTimeSetup = current is null;

        if (current is not null)
        {
            CountryId = current.CountryId;
            CityId = current.CityId;
            NeighborhoodId = current.NeighborhoodId;
        }

        await LoadPickerAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        if (CountryId is null)
        {
            ModelState.AddModelError(nameof(CountryId), UiText.Validation.CountryRequired);
        }

        if (CountyId is null)
        {
            ModelState.AddModelError(nameof(CountyId), UiText.Validation.CountyRequired);
        }

        if (CityId is null)
        {
            ModelState.AddModelError(nameof(CityId), UiText.Validation.LocalityRequired);
        }

        BrowsingLocation? resolved = null;
        if (ModelState.IsValid)
        {
            resolved = await _locations.ResolveAsync(CountryId, CountyId, CityId, NeighborhoodId, cancellationToken);
            if (resolved is null)
            {
                ModelState.AddModelError(string.Empty, UiText.Validation.InvalidLocation);
            }
        }

        if (resolved is null)
        {
            HasCurrentLocation = _context.Peek() is not null;
            await LoadPickerAsync(cancellationToken);
            return Page();
        }

        // The location is a setting, not a per-session choice: persist it and refresh the session.
        await _locations.SavePreferredAsync(userId, resolved, cancellationToken);
        _context.Set(resolved);

        TempData[TempDataKeys.Success] = UiText.Success.DefaultLocationSaved;

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Dashboard");
    }

    private async Task LoadPickerAsync(CancellationToken cancellationToken)
    {
        var cities = await _locations.GetCitiesAsync(cancellationToken);

        // Not stored on the profile (it's derivable from the city), so it's filled in here
        // whenever a city is already known but no county was posted/preselected yet.
        CountyId ??= cities.FirstOrDefault(c => c.Id == CityId)?.CountyId;

        Picker = new LocationPickerModel
        {
            Countries = await _locations.GetCountriesAsync(cancellationToken),
            Counties = await _locations.GetCountiesAsync(cancellationToken),
            Cities = cities,
            Neighborhoods = await _locations.GetNeighborhoodsAsync(cancellationToken),
            CountryId = CountryId,
            CountyId = CountyId,
            CityId = CityId,
            NeighborhoodId = NeighborhoodId,
            CountryField = nameof(CountryId),
            CountyField = nameof(CountyId),
            CityField = nameof(CityId),
            NeighborhoodField = nameof(NeighborhoodId),
            NeighborhoodEmptyLabel = UiText.Location.AllNeighborhoods,
            Inline = false
        };
    }
}
