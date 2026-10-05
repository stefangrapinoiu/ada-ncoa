using Microsoft.AspNetCore.Identity;

namespace DaMaiDeparte.Web.Models;

/// <summary>
/// There is exactly one kind of account. The same user can publish food today and reserve
/// food tomorrow, so no Donator/Beneficiary distinction is stored anywhere.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>When the user ticked the GDPR consent checkbox at registration. Always set once the
    /// account exists (the checkbox is required), kept nullable only because older seeded/test
    /// accounts predate this field.</summary>
    public DateTime? GdprConsentAt { get; set; }

    /// <summary>When the user accepted the Terms and Conditions. Null means they haven't yet — the
    /// next page they request redirects them to the acceptance screen (see RequireTermsAcceptedFilter).</summary>
    public DateTime? TermsAcceptedAt { get; set; }

    // ----- Preferred ("home") location -----
    // This is the location stored on the profile. It is only a default: the location the
    // user is currently browsing lives in the session and can differ (see BrowsingLocation).

    public int? PreferredCountryId { get; set; }

    public Country? PreferredCountry { get; set; }

    public int? PreferredCityId { get; set; }

    public City? PreferredCity { get; set; }

    public int? PreferredNeighborhoodId { get; set; }

    public Neighborhood? PreferredNeighborhood { get; set; }

    /// <summary>Food this user has published.</summary>
    public ICollection<DonationItem> Donations { get; set; } = new List<DonationItem>();

    /// <summary>Food this user has reserved.</summary>
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
