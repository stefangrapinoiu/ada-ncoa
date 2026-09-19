using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Receiver;

public class HistoryModel : PageModel
{
    private readonly IReservationService _reservations;
    private readonly UserManager<ApplicationUser> _userManager;

    public HistoryModel(IReservationService reservations, UserManager<ApplicationUser> userManager)
    {
        _reservations = reservations;
        _userManager = userManager;
    }

    public IReadOnlyList<ReceiverReservationItem> Items { get; private set; } = Array.Empty<ReceiverReservationItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _reservations.GetReceivedHistoryAsync(_userManager.GetUserId(User)!, cancellationToken);
    }
}
