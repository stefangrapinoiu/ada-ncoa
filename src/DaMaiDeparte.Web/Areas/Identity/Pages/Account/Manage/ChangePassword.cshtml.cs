using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Monitoring;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account.Manage;

public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditLog _audit;

    public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IAuditLog audit)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _audit = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = UiText.Validation.PasswordRequired)]
        [DataType(DataType.Password)]
        [Display(Name = "Parola actuală")]
        public string OldPassword { get; set; } = string.Empty;

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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        await _audit.WriteAsync(AuditEventType.PasswordChanged, user.Id, user.Email);
        TempData[TempDataKeys.Success] = UiText.Success.PasswordChanged;
        return RedirectToPage("./Index");
    }
}
