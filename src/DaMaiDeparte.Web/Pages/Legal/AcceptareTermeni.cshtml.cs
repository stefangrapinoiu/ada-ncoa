using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Legal;

/// <summary>
/// First-login interstitial: every authenticated request to a page outside /Legal is redirected
/// here by RequireTermsAcceptedFilter until the account has a TermsAcceptedAt. There is no way
/// to dismiss this without either accepting or logging out (see Logout link in the markup).
/// </summary>
public class AcceptareTermeniModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AcceptareTermeniModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        // Already accepted (e.g. the user navigated back here manually) — nothing to do.
        if (user.TermsAcceptedAt is not null)
        {
            return RedirectToSafeReturnUrl();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        user.TermsAcceptedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return RedirectToSafeReturnUrl();
    }

    private IActionResult RedirectToSafeReturnUrl() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Dashboard");
}
