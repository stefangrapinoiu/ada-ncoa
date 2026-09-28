using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donations;

/// <summary>
/// One details page for everybody. Because there is a single user type, what the page offers
/// depends on the relationship to this listing (owner / reserver / neither), never on a role.
/// </summary>
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

    /// <summary>The current user's own active reservation for this item, if any.</summary>
    public int? MyReservationId { get; private set; }

    /// <summary>The active reservation, loaded only for the owner.</summary>
    public Reservation? ActiveReservation { get; private set; }

    public bool IsExpired => Donation.Status == DonationStatus.Expired || Donation.ExpirationDate < RoDate.Today;

    public bool CanReserve =>
        !IsOwner && !IsExpired && Donation.Status == DonationStatus.Available;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        var donation = await _donations.GetDetailsAsync(Id, cancellationToken);
        if (donation is null)
        {
            return NotFound();
        }

        IsOwner = donation.DonatorId == userId;

        // A cancelled listing is only visible to its owner.
        if (donation.Status == DonationStatus.Cancelled && !IsOwner)
        {
            return NotFound();
        }

        Donation = donation;

        if (IsOwner)
        {
            var owned = await _donations.GetOwnedAsync(Id, userId, cancellationToken);
            ActiveReservation = owned?.Reservations.FirstOrDefault(r => r.CancelledAt == null);
        }
        else
        {
            MyReservationId = await _reservations.GetActiveReservationIdAsync(Id, userId, cancellationToken);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReserveAsync(CancellationToken cancellationToken)
    {
        var result = await _reservations.ReserveAsync(Id, _userManager.GetUserId(User)!, cancellationToken);

        if (result.Succeeded)
        {
            TempData[TempDataKeys.Success] = result.Message;
            return RedirectToPage("/Reservations/Details", new { id = result.Value });
        }

        if (result.Error == ServiceError.NotFound)
        {
            return NotFound();
        }

        // Forbidden here means "this is your own donation", which is a message, not a 403 page.
        TempData[TempDataKeys.Error] = result.Message;
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostCancelAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _donations.CancelAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        return HandleOwnerResult(result, returnUrl);
    }

    public async Task<IActionResult> OnPostCompleteAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _donations.CompleteAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        return HandleOwnerResult(result, returnUrl);
    }

    /// <summary>
    /// Donor only: releases the active reservation on this listing (e.g. the receiver stopped
    /// responding), same action as the "Anulează rezervarea" button on Reservations/Details —
    /// duplicated here too since this is the page donors land on first.
    /// </summary>
    public async Task<IActionResult> OnPostReleaseReservationAsync(int reservationId, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _reservations.ReleaseAsync(reservationId, _userManager.GetUserId(User)!, cancellationToken);
        return HandleOwnerResult(result, returnUrl);
    }

    private IActionResult HandleOwnerResult(ServiceResult result, string? returnUrl)
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
