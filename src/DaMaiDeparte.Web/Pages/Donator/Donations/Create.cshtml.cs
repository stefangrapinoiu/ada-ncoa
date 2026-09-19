using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DaMaiDeparte.Web.Pages.Donator.Donations;

[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
[RequestSizeLimit(10 * 1024 * 1024)]
public class CreateModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<Models.ApplicationUser> _userManager;

    public CreateModel(IDonationService donations, UserManager<Models.ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    [BindProperty]
    public DonationFormInput Input { get; set; } = new();

    public IEnumerable<SelectListItem> Categories { get; private set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Conditions { get; private set; } = Array.Empty<SelectListItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        var userId = _userManager.GetUserId(User)!;
        var result = await _donations.CreateAsync(userId, Input.ToServiceInput(), Input.Image, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == ServiceError.Forbidden)
            {
                return Forbid();
            }

            ModelState.AddModelError(result.Error == ServiceError.Validation && Input.Image is not null
                ? $"{nameof(Input)}.{nameof(Input.Image)}"
                : string.Empty, result.Message!);
            await LoadOptionsAsync(cancellationToken);
            return Page();
        }

        TempData[TempDataKeys.Success] = result.Message;
        return RedirectToPage("./Details", new { id = result.Value });
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        (Categories, Conditions) = await DonationFormViewModel.LoadOptionsAsync(_donations, cancellationToken);
    }
}
