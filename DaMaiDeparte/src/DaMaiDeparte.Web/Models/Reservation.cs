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

    public bool IsActive => CancelledAt == null;
}
