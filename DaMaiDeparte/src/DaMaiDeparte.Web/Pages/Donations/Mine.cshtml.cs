using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donations;

/// <summary>"Donațiile mele" — every listing this account has published, in any status.</summary>
public class MineModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public MineModel(IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    /// <summary>Optional status filter; null means active (Available + Reserved).</summary>
    [BindProperty(SupportsGet = true, Name = "stare")]
    public DonationStatus? Status { get; set; }

    public IReadOnlyList<MyDonationItem> Items { get; private set; } = Array.Empty<MyDonationItem>();

    public UserSummary Summary { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
        {
            Status = null;
        }

        var userId = _userManager.GetUserId(User)!;
        Summary = await _donations.GetSummaryAsync(userId, cancellationToken);
        Items = await _donations.GetMyDonationsAsync(userId, Status, cancellationToken);
    }
}
