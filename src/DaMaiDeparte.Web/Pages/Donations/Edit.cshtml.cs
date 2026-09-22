using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donations;

[RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
[RequestSizeLimit(20 * 1024 * 1024)]
public class EditModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly ILocationService _locations;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(IDonationService donations, ILocationService locations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _locations = locations;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public DonationFormInput Input { get; set; } = new();

    public DonationFormViewModel Form { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var donation = await _donations.GetOwnedAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (donation is null)
        {
            return NotFound();
        }

        if (donation.Status != DonationStatus.Available)
        {
            TempData[TempDataKeys.Error] = UiText.Errors.EditNotAllowed;
            return RedirectToPage("./Details", new { id = Id });
        }

        Input = DonationFormInput.From(donation);
        await LoadFormAsync(donation.Images.ToList(), cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var expiration = Input.ValidateAndParse(ModelState);

        if (!ModelState.IsValid || expiration is null)
        {
            return await RedisplayAsync(userId, cancellationToken);
        }

        var result = await _donations.UpdateAsync(
            userId,
            Id,
            Input.ToServiceInput(expiration.Value),
            Input.UploadedImages(),
            Input.RemoveImageIds,
            cancellationToken);

        if (result.Succeeded)
        {
            TempData[TempDataKeys.Success] = result.Message;
            return RedirectToPage("./Details", new { id = Id });
        }

        switch (result.Error)
        {
            case ServiceError.NotFound:
            case ServiceError.Forbidden:
                // Never reveal listings that belong to someone else.
                return NotFound();
            case ServiceError.InvalidState:
            case ServiceError.Conflict:
                TempData[TempDataKeys.Error] = result.Message;
                return RedirectToPage("./Details", new { id = Id });
            default:
                ModelState.AddModelError(string.Empty, result.Message!);
                return await RedisplayAsync(userId, cancellationToken);
        }
    }

    private async Task<IActionResult> RedisplayAsync(string userId, CancellationToken cancellationToken)
    {
        var donation = await _donations.GetOwnedAsync(Id, userId, cancellationToken);
        if (donation is null)
        {
            return NotFound();
        }

        await LoadFormAsync(donation.Images.ToList(), cancellationToken);
        return Page();
    }

    private async Task LoadFormAsync(IReadOnlyList<DonationImage> existingImages, CancellationToken cancellationToken)
    {
        var cities = await _locations.GetCitiesAsync(cancellationToken);

        // Not carried on the listing itself (the city already implies its județ); fill it in
        // from the listing's current city so editing doesn't force re-picking it.
        Input.CountyId ??= cities.FirstOrDefault(c => c.Id == Input.CityId)?.CountyId;

        Form = new DonationFormViewModel
        {
            Input = Input,
            Categories = await _donations.GetAllowedCategoriesAsync(cancellationToken),
            ExistingImages = existingImages,
            Picker = new LocationPickerModel
            {
                Countries = await _locations.GetCountriesAsync(cancellationToken),
                Counties = await _locations.GetCountiesAsync(cancellationToken),
                Cities = cities,
                Neighborhoods = await _locations.GetNeighborhoodsAsync(cancellationToken),
                CountryId = Input.CountryId,
                CountyId = Input.CountyId,
                CityId = Input.CityId,
                NeighborhoodId = Input.NeighborhoodId,
                CountryField = "Input.CountryId",
                CountyField = "Input.CountyId",
                CityField = "Input.CityId",
                NeighborhoodField = "Input.NeighborhoodId"
            }
        };
    }
}
