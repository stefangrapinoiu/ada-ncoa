using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Monitoring;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditLog _audit;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        IAuditLog audit,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _audit = audit;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = UiText.Validation.EmailRequired)]
        [EmailAddress(ErrorMessage = UiText.Validation.EmailInvalid)]
        [Display(Name = "Adresă de e-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.PasswordRequired)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ține-mă minte")]
        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturnUrl() ?? "/Dashboard");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Input.Email.Trim();
        var result = await _signInManager.PasswordSignInAsync(
            email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        // Security log (admin panel). The account lookup only feeds the log; the response the
        // visitor sees is identical whether or not the e-mail exists.
        var account = await _signInManager.UserManager.FindByEmailAsync(email);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out after failed sign-in attempts");
            await _audit.WriteAsync(AuditEventType.LockedOut, account?.Id, email);
            ModelState.AddModelError(string.Empty, UiText.Errors.LockedOut);
            return Page();
        }

        if (!result.Succeeded)
        {
            await _audit.WriteAsync(AuditEventType.LoginFailed, account?.Id, email,
                account is null ? "e-mail necunoscut" : "parolă greșită");
            ModelState.AddModelError(string.Empty, UiText.Errors.InvalidLogin);
            return Page();
        }

        await _audit.WriteAsync(AuditEventType.LoginSucceeded, account?.Id, email);

        // Straight to the feed. The location saved on the account is restored from the profile by
        // LocationContext, so the user is never asked to pick it again; the dashboard only
        // redirects to /Location if the account has no usable location at all.
        var returnUrl = SafeReturnUrl();

        return returnUrl is not null
            ? LocalRedirect(returnUrl)
            : RedirectToPage("/Dashboard", new { area = string.Empty });
    }

    private string? SafeReturnUrl() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;
}
