using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donator;

public class DashboardModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    public DonatorDashboard Data { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Data = await _donations.GetDashboardAsync(_userManager.GetUserId(User)!, cancellationToken);
    }
}
