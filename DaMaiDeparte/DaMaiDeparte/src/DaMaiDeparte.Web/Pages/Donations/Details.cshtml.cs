using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donations;

public class DetailsModel : PageModel
{
    private readonly IDonationService _donations;
    private readonly IReservationService _reservations;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(IDonationService donations, IReservationService reservations, UserManager<ApplicationUser> userManager)
    {
        _donations = donations;
        _reservations = reservations;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public DonationItem Donation { get; private set; } = null!;

    public bool IsOwner { get; private set; }

    /// <summary>The current receiver's own active reservation for this item, if any.</summary>
    public int? MyReservationId { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var donation = await _donations.GetPublicDetailsAsync(Id, cancellationToken);
        if (donation is null)
        {
            return NotFound();
        }

        Donation = donation;
        var userId = _userManager.GetUserId(User);
        IsOwner = userId is not null && donation.DonatorId == userId;

        if (userId is not null && User.IsInRole(AppRoles.Receiver) && donation.Status != DonationStatus.Available)
        {
            MyReservationId = await _reservations.GetActiveReservationIdAsync(Id, userId, cancellationToken);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReserveAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (!User.IsInRole(AppRoles.Receiver))
        {
            return Forbid();
        }

        var result = await _reservations.ReserveAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (!result.Succeeded)
        {
            switch (result.Error)
            {
                case ServiceError.NotFound:
                    return NotFound();
                case ServiceError.Forbidden:
                    return Forbid();
                default:
                    TempData[TempDataKeys.Error] = result.Message;
                    return RedirectToPage(new { id = Id });
            }
        }

        TempData[TempDataKeys.Success] = result.Message;
        return RedirectToPage("/Receiver/Reservations/Details", new { id = result.Value });
    }
}
