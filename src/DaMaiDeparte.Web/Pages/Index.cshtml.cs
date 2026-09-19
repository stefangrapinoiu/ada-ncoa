using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IDonationService _donations;

    public IndexModel(IDonationService donations)
    {
        _donations = donations;
    }

    public IReadOnlyList<DonationCard> Latest { get; private set; } = Array.Empty<DonationCard>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await _donations.SearchAvailableAsync(new DonationSearchQuery { PageSize = 4 }, cancellationToken);
        Latest = result.Items;
    }
}
