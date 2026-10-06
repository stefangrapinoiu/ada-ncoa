using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Notificari;

/// <summary>
/// "Mesaje noi" — every conversation with messages the user hasn't read yet. Each row links
/// straight to that reservation's chat; opening it clears just that conversation.
/// </summary>
public class IndexModel : PageModel
{
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(INotificationService notifications, UserManager<ApplicationUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    public IReadOnlyList<UnreadConversation> Conversations { get; private set; } = Array.Empty<UnreadConversation>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Conversations = await _notifications.GetUnreadConversationsAsync(_userManager.GetUserId(User)!, cancellationToken);
    }

    /// <summary>Polled by site.js about once a minute to refresh the bell badge without a reload.</summary>
    public async Task<IActionResult> OnGetCountAsync(CancellationToken cancellationToken)
    {
        var count = await _notifications.GetUnreadCountAsync(_userManager.GetUserId(User)!, cancellationToken);
        return new JsonResult(new { count });
    }
}
