using System.ComponentModel.DataAnnotations;
using System.Text;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

/// <summary>
/// Consumes the code + email pair from the link ForgotPasswordModel sends, and lets the user
/// choose a new password. An unknown e-mail and an invalid/expired token both end up showing
/// the same "link invalid" state, so this page can't be used to test which e-mail addresses
/// have an account.
/// </summary>
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool Submitted { get; private set; }

    public bool LinkInvalid { get; private set; }

    public class InputModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.PasswordRequired)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = UiText.Validation.PasswordLength)]
        [DataType(DataType.Password)]
        [Display(Name = "Parola nouă")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.Required)]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = UiText.Validation.PasswordsDoNotMatch)]
        [Display(Name = "Confirmă parola nouă")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? code, string? email)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
        {
            LinkInvalid = true;
            return Page();
        }

        Input.Code = code;
        Input.Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            // Same outward result as an invalid token — never reveal whether the account exists.
            LinkInvalid = true;
            return Page();
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        }
        catch (FormatException)
        {
            LinkInvalid = true;
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "InvalidToken"))
            {
                LinkInvalid = true;
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        Submitted = true;
        return Page();
    }
}
