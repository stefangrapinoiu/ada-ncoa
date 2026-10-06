using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Reservations;

/// <summary>"Rezervările mele" — food this account has reserved, plus what it has received.</summary>
public class IndexModel : PageModel
{
    private readonly IReservationService _reservations;
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IReservationService reservations, INotificationService notifications, UserManager<ApplicationUser> userManager)
    {
        _reservations = reservations;
        _notifications = notifications;
        _userManager = userManager;
    }

    /// <summary>"istoric" shows received food; anything else shows active reservations.</summary>
    [BindProperty(SupportsGet = true, Name = "tab")]
    public string? Tab { get; set; }

    public bool ShowHistory => string.Equals(Tab, "istoric", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<MyReservationItem> Items { get; private set; } = Array.Empty<MyReservationItem>();

    /// <summary>Unread message count per reservation id, for the "Mesaje noi" badges.</summary>
    public IReadOnlyDictionary<int, int> UnreadByReservation { get; private set; } = new Dictionary<int, int>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        Items = ShowHistory
            ? await _reservations.GetReceivedHistoryAsync(userId, cancellationToken)
            : await _reservations.GetActiveReservationsAsync(userId, cancellationToken);
        UnreadByReservation = await _notifications.GetUnreadCountsByReservationAsync(userId, cancellationToken);
    }
}
