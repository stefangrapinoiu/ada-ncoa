using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

/// <summary>
/// Registration asks for a name, credentials and a location. It deliberately does not ask
/// "Ești donator sau beneficiar?" — there is only one kind of account.
///
/// The new account is NOT signed in automatically: the user is sent to a confirmation page and
/// has to log in with the credentials they just chose. That confirms the password works and that
/// the e-mail address was typed correctly before any data is created under the account.
/// </summary>
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILocationService _locations;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        ILocationService locations,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _locations = locations;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public LocationPickerModel Picker { get; private set; } = null!;

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

        [Display(Name = "Țara")]
        public int? CountryId { get; set; }

        [Display(Name = "Județul")]
        public int? CountyId { get; set; }

        [Display(Name = "Localitatea")]
        public int? CityId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard", new { area = string.Empty });
        }

        await LoadPickerAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.CountryId is null)
        {
            ModelState.AddModelError("Input.CountryId", UiText.Validation.CountryRequired);
        }

        if (Input.CountyId is null)
        {
            ModelState.AddModelError("Input.CountyId", UiText.Validation.CountyRequired);
        }

        if (Input.CityId is null)
        {
            ModelState.AddModelError("Input.CityId", UiText.Validation.LocalityRequired);
        }

        BrowsingLocation? location = null;
        if (ModelState.IsValid)
        {
            // Cartier is not asked at registration (it is optional and chosen later, from
            // "Schimbă locația"), so it is always null here.
            location = await _locations.ResolveAsync(Input.CountryId, Input.CountyId, Input.CityId, null, cancellationToken);
            if (location is null)
            {
                ModelState.AddModelError(string.Empty, UiText.Validation.InvalidLocation);
            }
        }

        if (!ModelState.IsValid || location is null)
        {
            await LoadPickerAsync(cancellationToken);
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim(),
            PreferredCountryId = location.CountryId,
            PreferredCityId = location.CityId,
            PreferredNeighborhoodId = location.NeighborhoodId,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadPickerAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation("New account created ({UserId})", user.Id);

        // No automatic sign-in: the user confirms the account by logging in.
        // The location they chose is stored on the profile, so the first login lands on a feed
        // that already has content without asking for the location again.
        return RedirectToPage("./RegisterConfirmation", new { email = user.Email, returnUrl = ReturnUrl });
    }

    private async Task LoadPickerAsync(CancellationToken cancellationToken)
    {
        Picker = new LocationPickerModel
        {
            Countries = await _locations.GetCountriesAsync(cancellationToken),
            Counties = await _locations.GetCountiesAsync(cancellationToken),
            Cities = await _locations.GetCitiesAsync(cancellationToken),
            Neighborhoods = Array.Empty<Neighborhood>(),
            CountryId = Input.CountryId,
            CountyId = Input.CountyId,
            CityId = Input.CityId,
            CountryField = "Input.CountryId",
            CountyField = "Input.CountyId",
            CityField = "Input.CityId",
            ShowNeighborhood = false
        };
    }
}
