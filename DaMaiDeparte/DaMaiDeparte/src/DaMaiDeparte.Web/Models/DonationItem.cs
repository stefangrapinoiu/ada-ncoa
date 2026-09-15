namespace DaMaiDeparte.Web.Models;

public class DonationItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public ProductCondition Condition { get; set; }

    public string PickupArea { get; set; } = string.Empty;

    public DonationStatus Status { get; set; } = DonationStatus.Available;

    public string DonatorId { get; set; } = string.Empty;

    public ApplicationUser Donator { get; set; } = null!;

    /// <summary>Relative path under wwwroot, e.g. "uploads/donations/abc.jpg".</summary>
    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Regenerated automatically by
    /// <see cref="Data.ApplicationDbContext"/> on every update.
    /// </summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    /// <summary>
    /// All reservations ever made for this item (cancelled ones are kept as history).
    /// At most one reservation can be active (CancelledAt == null) — enforced by a filtered unique index.
    /// </summary>
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
