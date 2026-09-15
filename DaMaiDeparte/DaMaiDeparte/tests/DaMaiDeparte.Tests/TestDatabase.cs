using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DaMaiDeparte.Tests;

/// <summary>
/// In-memory SQLite database shared by several DbContext instances (one per "request").
/// SQLite supports transactions and filtered unique indexes, so business rules are exercised for real.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public const string DonatorId = "donator-1";
    public const string OtherDonatorId = "donator-2";
    public const string ReceiverId = "receiver-1";
    public const string OtherReceiverId = "receiver-2";

    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();

        var order = 0;
        foreach (var (key, name) in DbSeeder.Categories)
        {
            db.Categories.Add(new Category { Key = key, Name = name, SortOrder = ++order });
        }

        db.Users.AddRange(
            User(DonatorId, "Ioana", AccountType.Donator),
            User(OtherDonatorId, "Mihai", AccountType.Donator),
            User(ReceiverId, "Andrei", AccountType.Receiver),
            User(OtherReceiverId, "Elena", AccountType.Receiver));

        db.SaveChanges();
        CategoryId = db.Categories.OrderBy(c => c.Id).First().Id;
    }

    public int CategoryId { get; }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public DonationService CreateDonationService(ApplicationDbContext db, IFileStorageService? files = null) =>
        new(db, files ?? new FakeFileStorage(), NullLogger<DonationService>.Instance);

    public ReservationService CreateReservationService(ApplicationDbContext db) =>
        new(db, NullLogger<ReservationService>.Instance);

    public DonationInput ValidInput(string title = "Scaun de birou") =>
        new(title, "Scaun ergonomic, în stare bună.", CategoryId, ProductCondition.Good, "Cluj-Napoca, Mărăști");

    /// <summary>Creates an available donation owned by <paramref name="donatorId"/> and returns its id.</summary>
    public async Task<int> CreateDonationAsync(string donatorId = DonatorId, string title = "Scaun de birou")
    {
        await using var db = CreateContext();
        var result = await CreateDonationService(db).CreateAsync(donatorId, ValidInput(title), null);
        Assert.True(result.Succeeded, result.Message);
        return result.Value;
    }

    /// <summary>Creates a donation and reserves it for <paramref name="receiverId"/>; returns (donationId, reservationId).</summary>
    public async Task<(int DonationId, int ReservationId)> CreateReservedDonationAsync(string receiverId = ReceiverId)
    {
        var donationId = await CreateDonationAsync();
        await using var db = CreateContext();
        var result = await CreateReservationService(db).ReserveAsync(donationId, receiverId);
        Assert.True(result.Succeeded, result.Message);
        return (donationId, result.Value);
    }

    public void Dispose() => _connection.Dispose();

    private static ApplicationUser User(string id, string firstName, AccountType type) => new()
    {
        Id = id,
        UserName = $"{id}@example.local",
        NormalizedUserName = $"{id}@EXAMPLE.LOCAL",
        Email = $"{id}@example.local",
        NormalizedEmail = $"{id}@EXAMPLE.LOCAL",
        FirstName = firstName,
        LastName = "Test",
        AccountType = type,
        CreatedAt = DateTime.UtcNow
    };
}

public sealed class FakeFileStorage : IFileStorageService
{
    public List<string> Deleted { get; } = new();

    public Task<string?> ValidateImageAsync(IFormFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult($"uploads/donations/{Guid.NewGuid():N}.jpg");

    public void DeleteImage(string? relativePath)
    {
        if (relativePath is not null)
        {
            Deleted.Add(relativePath);
        }
    }
}
