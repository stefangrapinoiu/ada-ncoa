using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace DaMaiDeparte.Web.ViewComponents;

/// <summary>
/// The bell in the top bar with the unread-messages badge. Rendered only for signed-in users
/// (see _Layout); site.js refreshes the number about once a minute.
/// </summary>
public class UnreadMessagesBellViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;

    public UnreadMessagesBellViewComponent(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var count = userId is null ? 0 : await _notifications.GetUnreadCountAsync(userId, HttpContext.RequestAborted);
        return View(count);
    }
}
