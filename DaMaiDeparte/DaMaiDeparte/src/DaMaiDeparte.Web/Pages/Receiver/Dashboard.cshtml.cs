using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Receiver;

public class DashboardModel : PageModel
{
    private readonly IReservationService _reservations;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(IReservationService reservations, UserManager<ApplicationUser> userManager)
    {
        _reservations = reservations;
        _userManager = userManager;
    }

    public string FirstName { get; private set; } = string.Empty;

    public IReadOnlyList<ReceiverReservationItem> Items { get; private set; } = Array.Empty<ReceiverReservationItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        FirstName = user?.FirstName ?? string.Empty;
        Items = await _reservations.GetActiveForReceiverAsync(_userManager.GetUserId(User)!, cancellationToken);
    }
}
