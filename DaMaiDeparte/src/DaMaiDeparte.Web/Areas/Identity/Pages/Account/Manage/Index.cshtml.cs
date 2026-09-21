using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILocationService _locations;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILocationService locations)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _locations = locations;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    /// <summary>The user's home location, shown read-only; it is changed from /Location.</summary>
    public string? PreferredLocation { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = UiText.Validation.FirstNameRequired)]
        [StringLength(50, ErrorMessage = UiText.Validation.NameLength)]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.LastNameRequired)]
        [StringLength(50, ErrorMessage = UiText.Validation.NameLength)]
        [Display(Name = "Nume")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = UiText.Validation.PhoneInvalid)]
        [StringLength(20, ErrorMessage = UiText.Validation.PhoneInvalid)]
        [Display(Name = "Număr de telefon")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await LoadAsync(user);
        Input = new InputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await LoadAsync(user);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        user.FirstName = Input.FirstName.Trim();
        user.LastName = Input.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData[TempDataKeys.Success] = UiText.Success.AccountUpdated;
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Email = user.Email ?? string.Empty;
        CreatedAt = user.CreatedAt;

        var preferred = await _locations.ResolvePreferredAsync(user.Id);
        PreferredLocation = preferred?.Display;
    }
}
