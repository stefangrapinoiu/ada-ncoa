using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Receiver.Reservations;

public class DetailsModel : PageModel
{
    private readonly IReservationService _reservations;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(IReservationService reservations, UserManager<ApplicationUser> userManager)
    {
        _reservations = reservations;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Reservation Reservation { get; private set; } = null!;

    public bool IsActive => Reservation.CancelledAt is null && Reservation.DonationItem.Status == DonationStatus.Reserved;

    public bool IsCompleted => Reservation.CancelledAt is null && Reservation.DonationItem.Status == DonationStatus.Completed;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        // Only the receiver who made the reservation can load it.
        var reservation = await _reservations.GetForReceiverAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (reservation is null)
        {
            return NotFound();
        }

        Reservation = reservation;
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _reservations.CancelAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (!result.Succeeded && result.Error == ServiceError.NotFound)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? TempDataKeys.Success : TempDataKeys.Error] = result.Message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return result.Succeeded ? RedirectToPage("/Receiver/Dashboard") : RedirectToPage(new { id = Id });
    }
}
