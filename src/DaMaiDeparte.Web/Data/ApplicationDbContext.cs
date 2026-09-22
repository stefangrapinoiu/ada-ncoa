using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DaMaiDeparte.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<County> Counties => Set<County>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();

    public DbSet<FoodCategory> FoodCategories => Set<FoodCategory>();

    public DbSet<DonationItem> DonationItems => Set<DonationItem>();

    public DbSet<DonationImage> DonationImages => Set<DonationImage>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All timestamps are stored in UTC; make sure values read back are marked as UTC.
        // (This convention applies to both DateTime and DateTime? properties.)
        // DateOnly properties — the food expiry date — are untouched on purpose: they are
        // local calendar dates, not instants.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Country>(country =>
        {
            country.Property(c => c.Code).HasMaxLength(8).IsRequired();
            country.Property(c => c.Name).HasMaxLength(100).IsRequired();
            country.HasIndex(c => c.Code).IsUnique();
        });

        builder.Entity<County>(county =>
        {
            county.Property(c => c.Name).HasMaxLength(100).IsRequired();
            county.Property(c => c.Code).HasMaxLength(8).IsRequired();

            county.HasOne(c => c.Country)
                .WithMany(c => c.Counties)
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            // A code is unique inside its country (car-plate style: "CJ", "B", ...).
            county.HasIndex(c => new { c.CountryId, c.Code }).IsUnique();
        });

        builder.Entity<City>(city =>
        {
            city.Property(c => c.Name).HasMaxLength(100).IsRequired();
            city.Property(c => c.Slug).HasMaxLength(100).IsRequired();

            // Country is a denormalized convenience FK (the real hierarchy is Country → County →
            // City); Country itself only needs a back-reference to its counties.
            city.HasOne(c => c.Country)
                .WithMany()
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            city.HasOne(c => c.County)
                .WithMany(c => c.Cities)
                .HasForeignKey(c => c.CountyId)
                .OnDelete(DeleteBehavior.Restrict);

            // A slug is unique inside its country, so other countries may reuse a city name.
            city.HasIndex(c => new { c.CountryId, c.Slug }).IsUnique();
            city.HasIndex(c => c.IsActive);
            city.HasIndex(c => c.CountyId);
        });

        builder.Entity<Neighborhood>(neighborhood =>
        {
            neighborhood.Property(n => n.Name).HasMaxLength(100).IsRequired();

            neighborhood.HasOne(n => n.City)
                .WithMany(c => c.Neighborhoods)
                .HasForeignKey(n => n.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            neighborhood.HasIndex(n => new { n.CityId, n.Name }).IsUnique();
        });

        builder.Entity<FoodCategory>(category =>
        {
            category.Property(c => c.Key).HasMaxLength(50).IsRequired();
            category.Property(c => c.Name).HasMaxLength(100).IsRequired();
            category.Property(c => c.Description).HasMaxLength(300);
            category.HasIndex(c => c.Key).IsUnique();
        });

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(50).IsRequired();

            user.HasOne(u => u.PreferredCountry)
                .WithMany()
                .HasForeignKey(u => u.PreferredCountryId)
                .OnDelete(DeleteBehavior.Restrict);

            user.HasOne(u => u.PreferredCity)
                .WithMany()
                .HasForeignKey(u => u.PreferredCityId)
                .OnDelete(DeleteBehavior.Restrict);

            user.HasOne(u => u.PreferredNeighborhood)
                .WithMany()
                .HasForeignKey(u => u.PreferredNeighborhoodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DonationItem>(donation =>
        {
            donation.Property(d => d.Title).HasMaxLength(100).IsRequired();
            donation.Property(d => d.PickupLocation).HasMaxLength(200).IsRequired();
            donation.Property(d => d.PickupNotes).HasMaxLength(500);
            donation.Property(d => d.DonatorId).IsRequired();
            donation.Property(d => d.Version).IsConcurrencyToken();

            donation.HasOne(d => d.FoodCategory)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.FoodCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            donation.HasOne(d => d.Country)
                .WithMany()
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            donation.HasOne(d => d.City)
                .WithMany()
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            donation.HasOne(d => d.Neighborhood)
                .WithMany()
                .HasForeignKey(d => d.NeighborhoodId)
                .OnDelete(DeleteBehavior.Restrict);

            donation.HasOne(d => d.Donator)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.DonatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // The dashboard feed always filters on city (+ optional neighborhood), status and
            // expiry, so those are the columns worth indexing.
            donation.HasIndex(d => new { d.CityId, d.Status, d.ExpirationDate });
            donation.HasIndex(d => new { d.NeighborhoodId, d.Status });
            donation.HasIndex(d => d.Status);
            donation.HasIndex(d => d.ExpirationDate);
            donation.HasIndex(d => d.FoodCategoryId);
            donation.HasIndex(d => d.CreatedAt);
            donation.HasIndex(d => d.DonatorId);
        });

        builder.Entity<DonationImage>(image =>
        {
            image.Property(i => i.Path).HasMaxLength(300).IsRequired();

            image.HasOne(i => i.DonationItem)
                .WithMany(d => d.Images)
                .HasForeignKey(i => i.DonationItemId)
                .OnDelete(DeleteBehavior.Cascade);

            image.HasIndex(i => new { i.DonationItemId, i.SortOrder });
        });

        builder.Entity<Reservation>(reservation =>
        {
            reservation.Property(r => r.ReceiverId).IsRequired();
            reservation.Property(r => r.MeetingLocation).HasMaxLength(200);
            reservation.Property(r => r.Notes).HasMaxLength(500);

            reservation.HasOne(r => r.DonationItem)
                .WithMany(d => d.Reservations)
                .HasForeignKey(r => r.DonationItemId)
                .OnDelete(DeleteBehavior.Restrict);

            reservation.HasOne(r => r.Receiver)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Only one active (non-cancelled) reservation per donation item.
            // Square brackets are valid identifier quotes in both SQL Server and SQLite (tests).
            reservation.HasIndex(r => r.DonationItemId)
                .IsUnique()
                .HasFilter("[CancelledAt] IS NULL")
                .HasDatabaseName("IX_Reservations_DonationItemId_Active");

            reservation.HasIndex(r => r.ReceiverId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RefreshConcurrencyTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RefreshConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RefreshConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<DonationItem>())
        {
            if (entry.State == EntityState.Modified)
            {
                // The original value is still used in the UPDATE ... WHERE clause,
                // so a concurrent change results in DbUpdateConcurrencyException.
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }

    internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}
