namespace DaMaiDeparte.Web.Models;

/// <summary>
/// A single message in the simple per-reservation conversation between the donor and the
/// receiver — the in-app alternative to sharing phone numbers. Scoped to one reservation, not
/// a general inbox: both participants see the same thread, in order, on the reservation's own
/// page.
/// </summary>
public class ReservationMessage
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public Reservation Reservation { get; set; } = null!;

    public string SenderId { get; set; } = string.Empty;

    public ApplicationUser Sender { get; set; } = null!;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
