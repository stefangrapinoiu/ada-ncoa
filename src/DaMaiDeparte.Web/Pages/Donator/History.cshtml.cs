using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donator;

public class HistoryModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public HistoryModel(IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    /// <summary>"cancelled" shows the cancelled tab; anything else shows completed donations.</summary>
    [BindProperty(SupportsGet = true, Name = "tab")]
    public string? Tab { get; set; }

    public bool ShowCancelled => string.Equals(Tab, "cancelled", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<DonatorHistoryItem> Items { get; private set; } = Array.Empty<DonatorHistoryItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var status = ShowCancelled ? DonationStatus.Cancelled : DonationStatus.Completed;
        Items = await _donations.GetHistoryAsync(_userManager.GetUserId(User)!, status, cancellationToken);
    }
}
