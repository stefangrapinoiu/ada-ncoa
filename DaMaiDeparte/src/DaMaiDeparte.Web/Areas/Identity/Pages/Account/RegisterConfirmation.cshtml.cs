using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

/// <summary>
/// Shown after a successful registration. The account exists but is not signed in, so this page
/// exists to tell the user that and to hand them straight to the login form.
/// </summary>
public class RegisterConfirmationModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        // Reaching this page directly, without having just registered, is meaningless.
        if (string.IsNullOrWhiteSpace(Email))
        {
            return RedirectToPage("./Login");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard", new { area = string.Empty });
        }

        return Page();
    }
}
