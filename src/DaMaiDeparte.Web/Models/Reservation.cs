namespace DaMaiDeparte.Web.Models;

public class Reservation
{
    public int Id { get; set; }

    public int DonationItemId { get; set; }

    public DonationItem DonationItem { get; set; } = null!;

    public string ReceiverId { get; set; } = string.Empty;

    public ApplicationUser Receiver { get; set; } = null!;

    public DateTime ReservedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? MeetingLocation { get; set; }

    /// <summary>Stored in UTC.</summary>
    public DateTime? MeetingAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>The in-app message thread with the other participant for this reservation.</summary>
    public ICollection<ReservationMessage> Messages { get; set; } = new List<ReservationMessage>();

    /// <summary>
    /// When the donor last opened this reservation's conversation. Messages from the receiver
    /// newer than this are "unread" for the donor (bell badge, "Mesaje noi" list).
    /// </summary>
    public DateTime? DonorLastReadAt { get; set; }

    /// <summary>When the receiver last opened this reservation's conversation (see DonorLastReadAt).</summary>
    public DateTime? ReceiverLastReadAt { get; set; }

    public bool IsActive => CancelledAt == null;
}
