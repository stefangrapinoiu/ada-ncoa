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

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<DonationItem> DonationItems => Set<DonationItem>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All dates are stored in UTC; make sure values read back are marked as UTC.
        // (This convention applies to both DateTime and DateTime? properties.)
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(50).IsRequired();
            user.Property(u => u.AccountType).IsRequired();
        });

        builder.Entity<Category>(category =>
        {
            category.Property(c => c.Key).HasMaxLength(50).IsRequired();
            category.Property(c => c.Name).HasMaxLength(100).IsRequired();
            category.HasIndex(c => c.Key).IsUnique();
        });

        builder.Entity<DonationItem>(donation =>
        {
            donation.Property(d => d.Title).HasMaxLength(100).IsRequired();
            donation.Property(d => d.Description).HasMaxLength(2000).IsRequired();
            donation.Property(d => d.PickupArea).HasMaxLength(100).IsRequired();
            donation.Property(d => d.ImagePath).HasMaxLength(300);
            donation.Property(d => d.DonatorId).IsRequired();
            donation.Property(d => d.Version).IsConcurrencyToken();

            donation.HasOne(d => d.Category)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            donation.HasOne(d => d.Donator)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.DonatorId)
                .OnDelete(DeleteBehavior.Restrict);

            donation.HasIndex(d => d.Status);
            donation.HasIndex(d => d.CategoryId);
            donation.HasIndex(d => d.CreatedAt);
            donation.HasIndex(d => d.DonatorId);
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
