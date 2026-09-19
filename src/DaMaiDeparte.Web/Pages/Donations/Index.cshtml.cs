using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DaMaiDeparte.Web.Pages.Donations;

public class IndexModel : PageModel
{
    private readonly IDonationService _donations;

    public IndexModel(IDonationService donations)
    {
        _donations = donations;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "categorie")]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true, Name = "stare")]
    public ProductCondition? Condition { get; set; }

    [BindProperty(SupportsGet = true, Name = "zona")]
    public string? Area { get; set; }

    [BindProperty(SupportsGet = true, Name = "sort")]
    public DonationSort Sort { get; set; } = DonationSort.Newest;

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; } = 1;

    public PagedResult<DonationCard> Results { get; private set; } = null!;

    public IEnumerable<SelectListItem> Categories { get; private set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Conditions { get; private set; } = Array.Empty<SelectListItem>();

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) || CategoryId.HasValue || Condition.HasValue
        || !string.IsNullOrWhiteSpace(Area) || Sort != DonationSort.Newest;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Ignore invalid query values instead of failing.
        ModelState.Clear();
        if (Condition.HasValue && !Enum.IsDefined(Condition.Value))
        {
            Condition = null;
        }

        if (!Enum.IsDefined(Sort))
        {
            Sort = DonationSort.Newest;
        }

        Search = Truncate(Search, 100);
        Area = Truncate(Area, 100);

        var categories = await _donations.GetCategoriesAsync(cancellationToken);
        Categories = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(RoDate.Culture), c.Id == CategoryId))
            .ToList();
        Conditions = DisplayExtensions.ConditionOptions(Condition).ToList();

        Results = await _donations.SearchAvailableAsync(new DonationSearchQuery
        {
            Search = Search,
            CategoryId = CategoryId,
            Condition = Condition,
            Area = Area,
            Sort = Sort,
            Page = PageNumber
        }, cancellationToken);
    }

    /// <summary>Route values for a given page, preserving current filters.</summary>
    public Dictionary<string, string> RouteFor(int page)
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Search)) values["q"] = Search;
        if (CategoryId.HasValue) values["categorie"] = CategoryId.Value.ToString(RoDate.Culture);
        if (Condition.HasValue) values["stare"] = Condition.Value.ToString();
        if (!string.IsNullOrWhiteSpace(Area)) values["zona"] = Area;
        if (Sort != DonationSort.Newest) values["sort"] = Sort.ToString();
        if (page > 1) values["p"] = page.ToString(RoDate.Culture);
        return values;
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Trim() is var t && t.Length > max ? t[..max] : value.Trim();
}
