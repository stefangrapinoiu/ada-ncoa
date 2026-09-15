using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

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

        [Required(ErrorMessage = UiText.Validation.EmailRequired)]
        [EmailAddress(ErrorMessage = UiText.Validation.EmailInvalid)]
        [StringLength(256, ErrorMessage = UiText.Validation.EmailInvalid)]
        [Display(Name = "Adresă de e-mail")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = UiText.Validation.PhoneInvalid)]
        [StringLength(20, ErrorMessage = UiText.Validation.PhoneInvalid)]
        [Display(Name = "Număr de telefon")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = UiText.Validation.PasswordRequired)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = UiText.Validation.PasswordLength)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.Required)]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = UiText.Validation.PasswordsDoNotMatch)]
        [Display(Name = "Confirmă parola")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.AccountTypeRequired)]
        [Display(Name = "Tip cont")]
        public AccountType? AccountType { get; set; }
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.AccountType.HasValue && !Enum.IsDefined(Input.AccountType.Value))
        {
            ModelState.AddModelError("Input.AccountType", UiText.Validation.AccountTypeRequired);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var accountType = Input.AccountType!.Value;
        var user = new ApplicationUser
        {
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim(),
            AccountType = accountType,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        var role = accountType == Models.AccountType.Donator ? AppRoles.Donator : AppRoles.Receiver;
        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            _logger.LogError("Could not assign role {Role} to new user {UserId}", role, user.Id);
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, UiText.Errors.Generic);
            return Page();
        }

        _logger.LogInformation("New {AccountType} account created ({UserId})", accountType, user.Id);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return accountType == Models.AccountType.Donator
            ? RedirectToPage("/Donator/Dashboard", new { area = string.Empty })
            : RedirectToPage("/Donations/Index", new { area = string.Empty });
    }
}
