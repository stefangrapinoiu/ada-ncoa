using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donations;

/// <summary>
/// "Adaugă un aliment". Optimized for photograph → expiry → location → publish; the location
/// is pre-filled from the current browsing context so the donor rarely has to touch it.
/// </summary>
[RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
[RequestSizeLimit(20 * 1024 * 1024)]
public class CreateModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly ILocationService _locations;
    private readonly ILocationContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(
        IDonationService donations,
        ILocationService locations,
        ILocationContext context,
        UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _locations = locations;
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public DonationFormInput Input { get; set; } = new();

    public DonationFormViewModel Form { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var location = await _context.GetAsync(userId, cancellationToken);
        if (location is null)
        {
            return RedirectToPage("/Location", new { returnUrl = Url.Page("/Donations/Create") });
        }

        Input.CountryId = location.CountryId;
        Input.CityId = location.CityId;
        Input.NeighborhoodId = location.NeighborhoodId;

        await LoadFormAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var expiration = Input.ValidateAndParse(ModelState);

        if (!ModelState.IsValid || expiration is null)
        {
            await LoadFormAsync(cancellationToken);
            return Page();
        }

        var images = Input.UploadedImages();
        var result = await _donations.CreateAsync(userId, Input.ToServiceInput(expiration.Value), images, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            await LoadFormAsync(cancellationToken);
            return Page();
        }

        TempData[TempDataKeys.Success] = result.Message;
        return RedirectToPage("./Details", new { id = result.Value });
    }

    private async Task LoadFormAsync(CancellationToken cancellationToken)
    {
        Form = new DonationFormViewModel
        {
            Input = Input,
            Categories = await _donations.GetAllowedCategoriesAsync(cancellationToken),
            Picker = new LocationPickerModel
            {
                Countries = await _locations.GetCountriesAsync(cancellationToken),
                Cities = await _locations.GetCitiesAsync(cancellationToken),
                Neighborhoods = await _locations.GetNeighborhoodsAsync(cancellationToken),
                CountryId = Input.CountryId,
                CityId = Input.CityId,
                NeighborhoodId = Input.NeighborhoodId,
                CountryField = "Input.CountryId",
                CityField = "Input.CityId",
                NeighborhoodField = "Input.NeighborhoodId"
            }
        };
    }
}
