using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DaMaiDeparte.Web.Pages.Donator.Donations;

[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
[RequestSizeLimit(10 * 1024 * 1024)]
public class EditModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public DonationFormInput Input { get; set; } = new();

    public string? CurrentImagePath { get; private set; }

    public IEnumerable<SelectListItem> Categories { get; private set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Conditions { get; private set; } = Array.Empty<SelectListItem>();

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
        CurrentImagePath = donation.ImagePath;
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(userId, cancellationToken);
        }

        var result = await _donations.UpdateAsync(Id, userId, Input.ToServiceInput(), Input.Image, Input.RemoveImage, cancellationToken);
        if (result.Succeeded)
        {
            TempData[TempDataKeys.Success] = result.Message;
            return RedirectToPage("./Details", new { id = Id });
        }

        switch (result.Error)
        {
            case ServiceError.NotFound:
            case ServiceError.Forbidden:
                // Never reveal donations that belong to someone else.
                return NotFound();
            case ServiceError.InvalidState:
            case ServiceError.Conflict:
                TempData[TempDataKeys.Error] = result.Message;
                return RedirectToPage("./Details", new { id = Id });
            default:
                ModelState.AddModelError(Input.Image is not null ? "Input.Image" : string.Empty, result.Message!);
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

        CurrentImagePath = donation.ImagePath;
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        (Categories, Conditions) = await DonationFormViewModel.LoadOptionsAsync(_donations, cancellationToken);
    }
}
