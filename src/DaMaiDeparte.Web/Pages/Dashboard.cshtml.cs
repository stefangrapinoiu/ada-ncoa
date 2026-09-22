using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages;

/// <summary>
/// "Alimente disponibile" — the main screen after login. Everything shown here belongs to the
/// currently selected city; switching city is a session change, not an account change.
/// </summary>
public class DashboardModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly ILocationService _locations;
    private readonly ILocationContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(
        IDonationService donations,
        ILocationService locations,
        ILocationContext context,
        UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _locations = locations;
        _context = context;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true, Name = "cartier")]
    public int? NeighborhoodId { get; set; }

    [BindProperty(SupportsGet = true, Name = "stare")]
    public FeedFilter Filter { get; set; } = FeedFilter.All;

    [BindProperty(SupportsGet = true, Name = "categorie")]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; } = 1;

    public BrowsingLocation Location { get; private set; } = null!;

    public PagedResult<DonationCard> Results { get; private set; } = null!;

    public IReadOnlyList<Neighborhood> Neighborhoods { get; private set; } = Array.Empty<Neighborhood>();

    public IReadOnlyList<FoodCategory> Categories { get; private set; } = Array.Empty<FoodCategory>();

    public bool HasFilters =>
        NeighborhoodId.HasValue || Filter != FeedFilter.All || CategoryId.HasValue || !string.IsNullOrWhiteSpace(Search);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        var location = await _context.GetAsync(userId, cancellationToken);
        if (location is null)
        {
            // No location chosen yet — the feed has no meaning without one.
            return RedirectToPage("/Location", new { returnUrl = Url.Page("/Dashboard") });
        }

        Location = location;

        // Ignore invalid query values instead of failing the request.
        ModelState.Clear();
        if (!Enum.IsDefined(Filter))
        {
            Filter = FeedFilter.All;
        }

        Search = Truncate(Search, 100);

        Neighborhoods = await _locations.GetNeighborhoodsAsync(location.CityId, cancellationToken);
        Categories = await _donations.GetAllowedCategoriesAsync(cancellationToken);

        // The neighborhood filter defaults to the one in the browsing location, but only on the
        // very first visit (no "cartier" in the URL at all). Once the filter form has been
        // submitted, an explicit "Toate cartierele" choice (cartier="") must stick — otherwise
        // it always snaps back to the saved neighborhood and can never be cleared.
        var neighborhoodId = Request.Query.ContainsKey("cartier") ? NeighborhoodId : location.NeighborhoodId;
        if (neighborhoodId.HasValue && Neighborhoods.All(n => n.Id != neighborhoodId.Value))
        {
            neighborhoodId = null;
        }

        NeighborhoodId = neighborhoodId;

        Results = await _donations.SearchFeedAsync(
            new FeedQuery
            {
                CityId = location.CityId,
                NeighborhoodId = neighborhoodId,
                Filter = Filter,
                FoodCategoryId = CategoryId,
                Search = Search,
                Page = PageNumber
            },
            userId,
            cancellationToken);

        return Page();
    }

    /// <summary>Route values for a given page, preserving the current filters.</summary>
    public Dictionary<string, string> RouteFor(int page)
    {
        // "cartier" is always included (even empty) so an explicit "Toate cartierele" choice
        // survives pagination and tab links instead of silently reverting to the saved neighborhood.
        var values = new Dictionary<string, string>
        {
            ["cartier"] = NeighborhoodId?.ToString(RoDate.Culture) ?? string.Empty
        };
        if (Filter != FeedFilter.All) values["stare"] = Filter.ToString();
        if (CategoryId.HasValue) values["categorie"] = CategoryId.Value.ToString(RoDate.Culture);
        if (!string.IsNullOrWhiteSpace(Search)) values["q"] = Search;
        if (page > 1) values["p"] = page.ToString(RoDate.Culture);
        return values;
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }
}
