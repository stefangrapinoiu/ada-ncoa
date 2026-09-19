using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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
            return LocalRedirect(SafeReturnUrl() ?? "/");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email.Trim(), Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out after failed sign-in attempts");
            ModelState.AddModelError(string.Empty, UiText.Errors.LockedOut);
            return Page();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, UiText.Errors.InvalidLogin);
            return Page();
        }

        var returnUrl = SafeReturnUrl();
        if (returnUrl is not null)
        {
            return LocalRedirect(returnUrl);
        }

        var user = await _userManager.FindByNameAsync(Input.Email.Trim());
        return user?.AccountType == AccountType.Donator
            ? RedirectToPage("/Donator/Dashboard", new { area = string.Empty })
            : RedirectToPage("/Donations/Index", new { area = string.Empty });
    }

    private string? SafeReturnUrl() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;
}
