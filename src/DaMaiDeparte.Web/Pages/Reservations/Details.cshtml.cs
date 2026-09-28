using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages.Reservations;

/// <summary>
/// One reservation page for both participants. The viewer is whichever side of this
/// reservation they happen to be on — the same account can be a donor here and a receiver
/// on the next reservation.
/// </summary>
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

    /// <summary>True when the current user published the food; false when they reserved it.</summary>
    public bool IsDonator { get; private set; }

    public bool IsActive => Reservation.CancelledAt is null && Reservation.DonationItem.Status == DonationStatus.Reserved;

    public bool IsCompleted => Reservation.CancelledAt is null && Reservation.DonationItem.Status == DonationStatus.Completed;

    public class PickupInput
    {
        [Required(ErrorMessage = UiText.Validation.MeetingLocationRequired)]
        [StringLength(200, ErrorMessage = UiText.Validation.MeetingLocationLength)]
        [Display(Name = "Locul predării")]
        public string MeetingLocation { get; set; } = string.Empty;

        /// <summary>
        /// Date and time are separate text fields in dd/MM/yyyy and HH:mm. A native
        /// datetime-local input renders in the browser's locale, which shows mm/dd/yyyy and
        /// AM/PM on a US system.
        /// </summary>
        [Required(ErrorMessage = UiText.Validation.MeetingDateRequired)]
        [Display(Name = "Data predării")]
        public string? MeetingDate { get; set; }

        [Required(ErrorMessage = UiText.Validation.MeetingTimeRequired)]
        [Display(Name = "Ora predării")]
        public string? MeetingTime { get; set; }

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
            // Pre-filled from the handover details the donor gave when publishing.
            MeetingLocation = Reservation.MeetingLocation ?? Reservation.DonationItem.PickupLocation,
            MeetingDate = RoDate.ToDateInput(Reservation.MeetingAt),
            MeetingTime = RoDate.ToTimeInput(Reservation.MeetingAt),
            Notes = Reservation.Notes ?? Reservation.DonationItem.PickupNotes
        };

        return Page();
    }

    /// <summary>Donor only: sets where and when the food will be handed over.</summary>
    public async Task<IActionResult> OnPostPickupAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken) || !IsDonator)
        {
            return NotFound();
        }

        var date = RoDate.ParseDate(Input.MeetingDate);
        if (date is null && !string.IsNullOrWhiteSpace(Input.MeetingDate))
        {
            ModelState.AddModelError("Input.MeetingDate", UiText.Validation.DateInvalid);
        }

        var time = RoDate.ParseTime(Input.MeetingTime);
        if (time is null && !string.IsNullOrWhiteSpace(Input.MeetingTime))
        {
            ModelState.AddModelError("Input.MeetingTime", UiText.Validation.TimeInvalid);
        }

        DateTime? meetingAtUtc = null;
        if (date is not null && time is not null)
        {
            meetingAtUtc = RoDate.LocalToUtc(date.Value.ToDateTime(time.Value));
            if (meetingAtUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError("Input.MeetingDate", UiText.Validation.MeetingAtInPast);
            }
        }

        if (!ModelState.IsValid || meetingAtUtc is null)
        {
            return Page();
        }

        var result = await _reservations.SetPickupDetailsAsync(
            Id,
            _userManager.GetUserId(User)!,
            new PickupDetailsInput(Input.MeetingLocation, meetingAtUtc.Value, Input.Notes),
            cancellationToken);

        if (result.Succeeded)
        {
            TempData[TempDataKeys.Success] = result.Message;
            return RedirectToPage(new { id = Id });
        }

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

    /// <summary>Donor only: confirms the handover and archives the donation.</summary>
    public async Task<IActionResult> OnPostCompleteAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken) || !IsDonator)
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
            ? RedirectToPage("/Donations/Mine", new { stare = nameof(DonationStatus.Completed) })
            : RedirectToPage(new { id = Id });
    }

    /// <summary>Receiver only: releases the food so somebody else can take it.</summary>
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

        return result.Succeeded ? RedirectToPage("./Index") : RedirectToPage(new { id = Id });
    }

    /// <summary>
    /// Donor only: releases a reservation on their own listing, e.g. when the receiver stops
    /// responding. There is no way for the donor to complete the handover otherwise, so without
    /// this the listing would stay stuck as Reserved indefinitely.
    /// </summary>
    public async Task<IActionResult> OnPostReleaseAsync(CancellationToken cancellationToken)
    {
        var result = await _reservations.ReleaseAsync(Id, _userManager.GetUserId(User)!, cancellationToken);
        if (!result.Succeeded && result.Error == ServiceError.NotFound)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? TempDataKeys.Success : TempDataKeys.Error] = result.Message;
        return RedirectToPage(new { id = Id });
    }

    /// <summary>
    /// Loads the reservation from whichever side the current user is on. Both queries filter
    /// by user id, so a third party can never load somebody else's reservation.
    /// </summary>
    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;

        var asReceiver = await _reservations.GetForReceiverAsync(Id, userId, cancellationToken);
        if (asReceiver is not null)
        {
            Reservation = asReceiver;
            IsDonator = false;
            return true;
        }

        var asDonator = await _reservations.GetForDonatorAsync(Id, userId, cancellationToken);
        if (asDonator is not null)
        {
            Reservation = asDonator;
            IsDonator = true;
            return true;
        }

        return false;
    }
}
