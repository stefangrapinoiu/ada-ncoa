using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Donator.Reservations;

public class DetailsModel : PageModel
{
    private readonly IReservationService _reservations;
    private readonly IDonationService _donations;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(IReservationService reservations, IDonationService donations, UserManager<ApplicationUser> userManager)
    {
        _reservations = reservations;
        _donations = donations;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public PickupInput Input { get; set; } = new();

    public Reservation Reservation { get; private set; } = null!;

    /// <summary>True while the reservation is active and the donation is still reserved.</summary>
    public bool CanEdit => Reservation.CancelledAt is null && Reservation.DonationItem.Status == DonationStatus.Reserved;

    public class PickupInput
    {
        [Required(ErrorMessage = UiText.Validation.MeetingLocationRequired)]
        [StringLength(200, ErrorMessage = UiText.Validation.MeetingLocationLength)]
        [Display(Name = "Locul predării")]
        public string MeetingLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = UiText.Validation.MeetingAtRequired)]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data și ora")]
        public DateTime? MeetingAt { get; set; }

        [StringLength(500, ErrorMessage = UiText.Validation.NotesLength)]
        [Display(Name = "Instrucțiuni suplimentare")]
        public string? Notes { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken))
        {
            return NotFound();
        }

        Input = new PickupInput
        {
            MeetingLocation = Reservation.MeetingLocation ?? string.Empty,
            MeetingAt = Reservation.MeetingAt.HasValue ? RoDate.ToLocal(Reservation.MeetingAt.Value) : null,
            Notes = Reservation.Notes
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken))
        {
            return NotFound();
        }

        DateTime? meetingAtUtc = null;
        if (Input.MeetingAt.HasValue)
        {
            meetingAtUtc = RoDate.LocalToUtc(Input.MeetingAt.Value);
            if (meetingAtUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError("Input.MeetingAt", UiText.Validation.MeetingAtInPast);
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _reservations.SetPickupDetailsAsync(
            Id,
            _userManager.GetUserId(User)!,
            new PickupDetailsInput(Input.MeetingLocation, meetingAtUtc!.Value, Input.Notes),
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == ServiceError.NotFound)
            {
                return NotFound();
            }

            if (result.Error == ServiceError.Validation)
            {
                ModelState.AddModelError(string.Empty, result.Message!);
                return Page();
            }

            TempData[TempDataKeys.Error] = result.Message;
            return RedirectToPage(new { id = Id });
        }

        TempData[TempDataKeys.Success] = result.Message;
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken))
        {
            return NotFound();
        }

        var result = await _donations.CompleteAsync(Reservation.DonationItemId, _userManager.GetUserId(User)!, cancellationToken);
        if (!result.Succeeded && result.Error is ServiceError.NotFound or ServiceError.Forbidden)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? TempDataKeys.Success : TempDataKeys.Error] = result.Message;
        return result.Succeeded
            ? RedirectToPage("/Donator/History")
            : RedirectToPage(new { id = Id });
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        // Ownership is enforced in the query: only the donator of the item can load it.
        var reservation = await _reservations.GetForDonatorAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (reservation is null)
        {
            return false;
        }

        Reservation = reservation;
        return true;
    }
}
