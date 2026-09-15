using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donator.Donations;

public class IndexModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    /// <summary>Optional status filter; null means active (available + reserved).</summary>
    [BindProperty(SupportsGet = true, Name = "status")]
    public DonationStatus? Status { get; set; }

    public IReadOnlyList<DonatorActiveItem> Items { get; private set; } = Array.Empty<DonatorActiveItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
        {
            Status = null;
        }

        Items = await _donations.GetMyDonationsAsync(_userManager.GetUserId(User)!, Status, cancellationToken);
    }
}
