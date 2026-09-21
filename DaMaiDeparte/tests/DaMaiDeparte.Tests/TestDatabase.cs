using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Infrastructure;
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
///
/// There is only one kind of account: the ids below name the role a user happens to play in a
/// given test, not a permission level.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public const string DonatorId = "user-1";
    public const string OtherDonatorId = "user-2";
    public const string ReceiverId = "user-3";
    public const string OtherReceiverId = "user-4";

    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();

        var romania = new Country { Code = "RO", Name = "România", IsActive = true };
        db.Countries.Add(romania);
        db.SaveChanges();

        var cluj = new City { CountryId = romania.Id, Name = "Cluj-Napoca", Slug = "cluj-napoca", IsActive = true };
        var brasov = new City { CountryId = romania.Id, Name = "Brașov", Slug = "brasov", IsActive = true };
        db.Cities.AddRange(cluj, brasov);
        db.SaveChanges();

        var marasti = new Neighborhood { CityId = cluj.Id, Name = "Mărăști", IsActive = true };
        var gheorgheni = new Neighborhood { CityId = cluj.Id, Name = "Gheorgheni", IsActive = true };
        var centruBrasov = new Neighborhood { CityId = brasov.Id, Name = "Centru", IsActive = true };
        db.Neighborhoods.AddRange(marasti, gheorgheni, centruBrasov);

        var order = 0;
        foreach (var (key, name, description, isAllowed) in DbSeeder.FoodCategories)
        {
            db.FoodCategories.Add(new FoodCategory
            {
                Key = key,
                Name = name,
                Description = description,
                IsActive = true,
                IsAllowed = isAllowed,
                SortOrder = ++order
            });
        }

        db.Users.AddRange(
            User(DonatorId, "Ioana", romania.Id, cluj.Id, marasti.Id),
            User(OtherDonatorId, "Mihai", romania.Id, cluj.Id, null),
            User(ReceiverId, "Andrei", romania.Id, cluj.Id, gheorgheni.Id),
            User(OtherReceiverId, "Elena", romania.Id, cluj.Id, null));

        db.SaveChanges();

        CountryId = romania.Id;
        ClujId = cluj.Id;
        BrasovId = brasov.Id;
        MarastiId = marasti.Id;
        GheorgheniId = gheorgheni.Id;
        BrasovCentruId = centruBrasov.Id;

        AllowedCategoryId = db.FoodCategories.Single(c => c.Key == "PackagedBakery").Id;
        OtherApprovedCategoryId = db.FoodCategories.Single(c => c.Key == "OtherApproved").Id;
        MeatCategoryId = db.FoodCategories.Single(c => c.Key == "Meat").Id;
        DairyCategoryId = db.FoodCategories.Single(c => c.Key == "Dairy").Id;
    }

    public int CountryId { get; }

    public int ClujId { get; }

    public int BrasovId { get; }

    public int MarastiId { get; }

    public int GheorgheniId { get; }

    public int BrasovCentruId { get; }

    public int AllowedCategoryId { get; }

    public int OtherApprovedCategoryId { get; }

    public int MeatCategoryId { get; }

    public int DairyCategoryId { get; }

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

    public LocationService CreateLocationService(ApplicationDbContext db) => new(db);

    /// <summary>
    /// A listing that satisfies every rule: allowed category, packaged food, 10 days of shelf
    /// life, a valid Cluj-Napoca location and handover details.
    /// </summary>
    public DonationInput ValidInput(string title = "Pâine integrală feliată", int? cityId = null, int? neighborhoodId = null) =>
        new(title,
            AllowedCategoryId,
            RoDate.Today.AddDays(10),
            CountryId,
            cityId ?? ClujId,
            neighborhoodId ?? MarastiId,
            "Stația de tramvai Mărăști",
            "Zilnic după ora 18:00.");

    public static IReadOnlyList<IFormFile> OneImage() => new[] { FakeFormFile.Create("foto1.jpg") };

    public static IReadOnlyList<IFormFile> Images(int count) =>
        Enumerable.Range(1, count).Select(i => FakeFormFile.Create($"foto{i}.jpg")).ToList();

    /// <summary>Creates an available donation owned by <paramref name="donatorId"/> and returns its id.</summary>
    public async Task<int> CreateDonationAsync(
        string donatorId = DonatorId,
        string title = "Pâine integrală feliată",
        int? cityId = null,
        int? neighborhoodId = null)
    {
        await using var db = CreateContext();
        var result = await CreateDonationService(db)
            .CreateAsync(donatorId, ValidInput(title, cityId, neighborhoodId), OneImage());
        Assert.True(result.Succeeded, result.Message);
        return result.Value;
    }

    /// <summary>Creates a donation and reserves it; returns (donationId, reservationId).</summary>
    public async Task<(int DonationId, int ReservationId)> CreateReservedDonationAsync(string receiverId = ReceiverId)
    {
        var donationId = await CreateDonationAsync();
        await using var db = CreateContext();
        var result = await CreateReservationService(db).ReserveAsync(donationId, receiverId);
        Assert.True(result.Succeeded, result.Message);
        return (donationId, result.Value);
    }

    /// <summary>Feed query for a city, with the defaults the dashboard uses.</summary>
    public FeedQuery Feed(int? cityId = null, int? neighborhoodId = null, FeedFilter filter = FeedFilter.All) =>
        new() { CityId = cityId ?? ClujId, NeighborhoodId = neighborhoodId, Filter = filter, PageSize = 50 };

    public void Dispose() => _connection.Dispose();

    private static ApplicationUser User(string id, string firstName, int countryId, int cityId, int? neighborhoodId) => new()
    {
        Id = id,
        UserName = $"{id}@example.local",
        NormalizedUserName = $"{id}@EXAMPLE.LOCAL",
        Email = $"{id}@example.local",
        NormalizedEmail = $"{id}@EXAMPLE.LOCAL",
        FirstName = firstName,
        LastName = "Test",
        PreferredCountryId = countryId,
        PreferredCityId = cityId,
        PreferredNeighborhoodId = neighborhoodId,
        CreatedAt = DateTime.UtcNow
    };
}

/// <summary>Minimal IFormFile so the services can be exercised without touching disk.</summary>
public sealed class FakeFormFile : IFormFile
{
    private readonly byte[] _content;

    private FakeFormFile(string fileName, byte[] content)
    {
        FileName = fileName;
        _content = content;
    }

    public static IFormFile Create(string fileName = "foto.jpg") =>
        // A valid JPEG signature, so even a real validator would accept it.
        new FakeFormFile(fileName, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 });

    public string ContentType => "image/jpeg";

    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";

    public IHeaderDictionary Headers { get; } = new HeaderDictionary();

    public long Length => _content.Length;

    public string Name => "file";

    public string FileName { get; }

    public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
        target.WriteAsync(_content, 0, _content.Length, cancellationToken);

    public Stream OpenReadStream() => new MemoryStream(_content, writable: false);
}

public sealed class FakeFileStorage : IFileStorageService
{
    public List<string> Deleted { get; } = new();

    public List<string> Saved { get; } = new();

    public Task<string?> ValidateImageAsync(IFormFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var path = $"uploads/donations/{Guid.NewGuid():N}.jpg";
        Saved.Add(path);
        return Task.FromResult(path);
    }

    public void DeleteImage(string? relativePath)
    {
        if (relativePath is not null)
        {
            Deleted.Add(relativePath);
        }
    }
}
