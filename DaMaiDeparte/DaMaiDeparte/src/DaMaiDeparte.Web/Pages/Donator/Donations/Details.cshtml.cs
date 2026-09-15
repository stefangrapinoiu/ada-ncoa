using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donator.Donations;

public class DetailsModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public DonationItem Donation { get; private set; } = null!;

    public Reservation? ActiveReservation { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var donation = await _donations.GetOwnedAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (donation is null)
        {
            return NotFound();
        }

        Donation = donation;
        ActiveReservation = donation.Reservations.FirstOrDefault(r => r.CancelledAt == null);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _donations.CancelAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        return HandleResult(result, returnUrl);
    }

    public async Task<IActionResult> OnPostCompleteAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _donations.CompleteAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        return HandleResult(result, returnUrl);
    }

    private IActionResult HandleResult(ServiceResult result, string? returnUrl)
    {
        if (!result.Succeeded && result.Error is ServiceError.NotFound or ServiceError.Forbidden)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? TempDataKeys.Success : TempDataKeys.Error] = result.Message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage(new { id = Id });
    }
}
