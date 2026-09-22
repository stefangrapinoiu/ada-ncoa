using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages;

/// <summary>
/// Public landing page. The feed itself lives behind authentication because it is
/// location-scoped, so a signed-in visitor is sent straight to their dashboard.
/// </summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Dashboard")
            : Page();
}
