using Microsoft.AspNetCore.Identity;

namespace DaMaiDeparte.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<DonationItem> Donations { get; set; } = new List<DonationItem>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
