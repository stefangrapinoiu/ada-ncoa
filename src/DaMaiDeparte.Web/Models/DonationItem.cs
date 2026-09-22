namespace DaMaiDeparte.Web.Models;

/// <summary>
/// A single packaged food product offered by one household to another.
/// Location is structured (country → city → optional neighborhood) and the expiry date is
/// a local calendar date, never a timestamp.
/// </summary>
public class DonationItem
{
    public int Id { get; set; }

    /// <summary>Short product name, e.g. "Pâine integrală feliată".</summary>
    public string Title { get; set; } = string.Empty;

    public int FoodCategoryId { get; set; }

    public FoodCategory FoodCategory { get; set; } = null!;

    /// <summary>
    /// Expiry / best-before date printed on the packaging. Stored as a date only because it
    /// is a Romanian local calendar date, not an instant in time.
    /// </summary>
    public DateOnly ExpirationDate { get; set; }

    // ----- Location -----

    public int CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public int CityId { get; set; }

    public City City { get; set; } = null!;

    /// <summary>Optional: a donation always has a city but may have no neighborhood.</summary>
    public int? NeighborhoodId { get; set; }

    public Neighborhood? Neighborhood { get; set; }

    // ----- Handover ("Detaliile predării") -----
    // Asked for when the listing is created, so a receiver knows where the food can be picked up
    // before reserving it. The exact time is agreed afterwards, on the reservation.

    /// <summary>A public meeting point, e.g. "Intrarea principală Iulius Mall".</summary>
    public string PickupLocation { get; set; } = string.Empty;

    /// <summary>Optional availability or instructions, e.g. "zilnic după ora 18:00".</summary>
    public string? PickupNotes { get; set; }

    public DonationStatus Status { get; set; } = DonationStatus.Available;

    /// <summary>The user who published this food. Any user can be in this role.</summary>
    public string DonatorId { get; set; } = string.Empty;

    public ApplicationUser Donator { get; set; } = null!;

    /// <summary>When the donor confirmed the food-safety rules (packaged, sealed, no meat/dairy).</summary>
    public DateTime SafetyConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    /// <summary>Set when the listing was moved to <see cref="DonationStatus.Expired"/>.</summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Regenerated automatically by
    /// <see cref="Data.ApplicationDbContext"/> on every update.
    /// </summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    /// <summary>One to three photos of the product, ordered by <see cref="DonationImage.SortOrder"/>.</summary>
    public ICollection<DonationImage> Images { get; set; } = new List<DonationImage>();

    /// <summary>
    /// All reservations ever made for this item (cancelled ones are kept as history).
    /// At most one reservation can be active (CancelledAt == null) — enforced by a filtered unique index.
    /// </summary>
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

/// <summary>A stored photo belonging to a donation.</summary>
public class DonationImage
{
    public int Id { get; set; }

    public int DonationItemId { get; set; }

    public DonationItem DonationItem { get; set; } = null!;

    /// <summary>Relative path under wwwroot, e.g. "uploads/donations/abc.jpg".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>0-based display order; 0 is the cover photo.</summary>
    public int SortOrder { get; set; }
}
