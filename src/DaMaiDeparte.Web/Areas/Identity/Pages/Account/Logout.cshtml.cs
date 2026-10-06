using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Monitoring;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditLog _audit;

    public LogoutModel(SignInManager<ApplicationUser> signInManager, IAuditLog audit)
    {
        _signInManager = signInManager;
        _audit = audit;
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        await _audit.WriteAsync(AuditEventType.Logout, _signInManager.UserManager.GetUserId(User), User.Identity?.Name);
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index", new { area = string.Empty });
    }
}
