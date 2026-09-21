using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Tests;

public class DonationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Any_user_can_publish_food()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var result = await service.CreateAsync(TestDatabase.DonatorId, _database.ValidInput(), TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(UiText.Success.DonationPublished, result.Message);

        var stored = await db.DonationItems.AsNoTracking().Include(d => d.Images).SingleAsync(d => d.Id == result.Value);
        Assert.Equal(DonationStatus.Available, stored.Status);
        Assert.Equal(TestDatabase.DonatorId, stored.DonatorId);
        Assert.Equal(_database.ClujId, stored.CityId);
        Assert.Single(stored.Images);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
    }

    [Fact]
    public async Task Same_account_can_publish_and_reserve()
    {
        // The core "one user type" rule: no account flag decides who may do what.
        var mine = await _database.CreateDonationAsync(TestDatabase.DonatorId);
        var theirs = await _database.CreateDonationAsync(TestDatabase.OtherDonatorId, "Paste penne");

        await using var db = _database.CreateContext();
        var reserved = await _database.CreateReservationService(db).ReserveAsync(theirs, TestDatabase.DonatorId);

        Assert.True(reserved.Succeeded, reserved.Message);
        Assert.True(await db.DonationItems.AnyAsync(d => d.Id == mine && d.DonatorId == TestDatabase.DonatorId));
    }

    // ---------- Food safety ----------

    [Fact]
    public async Task Prohibited_category_cannot_be_used()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var meat = await service.CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput("Conservă") with { FoodCategoryId = _database.MeatCategoryId },
            TestDatabase.OneImage());

        var dairy = await service.CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput("Cutie") with { FoodCategoryId = _database.DairyCategoryId },
            TestDatabase.OneImage());

        Assert.Equal(ServiceError.Validation, meat.Error);
        Assert.Equal(UiText.Validation.CategoryNotAllowed, meat.Message);
        Assert.Equal(ServiceError.Validation, dairy.Error);
        Assert.False(await db.DonationItems.AnyAsync());
    }

    [Theory]
    [InlineData("Salam de Sibiu ambalat")]
    [InlineData("Pate de ficat")]
    [InlineData("Conservă de ton în ulei")]
    [InlineData("Mezeluri asortate")]
    public async Task Meat_is_rejected_even_in_an_allowed_category(string title)
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db)
            .CreateAsync(TestDatabase.DonatorId, _database.ValidInput(title), TestDatabase.OneImage());

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Equal(UiText.Errors.ProhibitedMeat, result.Message);
    }

    [Theory]
    [InlineData("Iaurt natural 400 g")]
    [InlineData("Cascaval ambalat")]
    [InlineData("Lapte UHT 1,5%")]
    [InlineData("Unt 200 g")]
    public async Task Dairy_is_rejected_even_in_an_allowed_category(string title)
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db)
            .CreateAsync(TestDatabase.DonatorId, _database.ValidInput(title), TestDatabase.OneImage());

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Equal(UiText.Errors.ProhibitedDairy, result.Message);
    }

    [Theory]
    [InlineData("Unt de arahide 350 g")]
    [InlineData("Lapte de migdale fără zahăr")]
    public async Task Plant_based_products_are_not_mistaken_for_dairy(string title)
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db)
            .CreateAsync(TestDatabase.DonatorId, _database.ValidInput(title), TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);
    }

    [Fact]
    public async Task Prohibited_words_in_the_handover_notes_are_also_rejected()
    {
        // There is no free-text description any more, so the screen reads the title and the
        // handover notes — the only free text a donor can still write.
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput("Pachet asortat") with { PickupNotes = "Pot aduce și niște șuncă." },
            TestDatabase.OneImage());

        Assert.Equal(UiText.Errors.ProhibitedMeat, result.Message);
    }

    // ---------- Handover details, collected at creation ----------

    [Fact]
    public async Task Handover_location_is_required_at_creation()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { PickupLocation = "   " },
            TestDatabase.OneImage());

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Equal(UiText.Validation.PickupLocationRequired, result.Message);
    }

    [Fact]
    public async Task Handover_details_are_stored_with_the_listing()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { PickupLocation = "Piața Unirii, lângă statuie", PickupNotes = "Zilnic după 18:00." },
            TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);

        var stored = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == result.Value);
        Assert.Equal("Piața Unirii, lângă statuie", stored.PickupLocation);
        Assert.Equal("Zilnic după 18:00.", stored.PickupNotes);
    }

    [Fact]
    public async Task Handover_notes_are_optional()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { PickupNotes = null },
            TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);
        Assert.Null((await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == result.Value)).PickupNotes);
    }

    [Fact]
    public async Task Catch_all_category_is_still_usable()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput("Cutie de cereale sigilată") with { FoodCategoryId = _database.OtherApprovedCategoryId },
            TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);
    }

    // ---------- Expiry ----------

    [Fact]
    public async Task Expired_food_cannot_be_published()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { ExpirationDate = RoDate.Today.AddDays(-1) },
            TestDatabase.OneImage());

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Equal(UiText.Validation.Expired, result.Message);
    }

    [Theory]
    [InlineData(0)] // expires today
    [InlineData(1)] // expires tomorrow
    [InlineData(2)] // expires the day after tomorrow
    public async Task Food_below_the_minimum_shelf_life_is_rejected(int daysAhead)
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { ExpirationDate = RoDate.Today.AddDays(daysAhead) },
            TestDatabase.OneImage());

        Assert.Equal(ServiceError.Validation, result.Error);
        Assert.Equal(UiText.Validation.ExpiresTooSoon, result.Message);
    }

    [Fact]
    public async Task Exactly_three_days_of_shelf_life_is_accepted()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { ExpirationDate = RoDate.Today.AddDays(FoodRules.MinimumShelfLifeDays) },
            TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);
    }

    [Fact]
    public async Task Due_listings_become_expired_and_leave_the_feed()
    {
        var id = await _database.CreateDonationAsync();

        // Move the expiry into the past behind the service's back.
        await using (var db = _database.CreateContext())
        {
            var donation = await db.DonationItems.SingleAsync(d => d.Id == id);
            donation.ExpirationDate = RoDate.Today.AddDays(-1);
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        var service = _database.CreateDonationService(check);

        // Already invisible, even before the sweep runs.
        var beforeSweep = await service.SearchFeedAsync(_database.Feed(), TestDatabase.ReceiverId);
        Assert.DoesNotContain(beforeSweep.Items, i => i.Id == id);

        Assert.Equal(1, await service.ExpireDueDonationsAsync());

        var stored = await check.DonationItems.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal(DonationStatus.Expired, stored.Status);
        Assert.NotNull(stored.ExpiredAt);

        // Archived, not deleted.
        Assert.True(await check.DonationItems.AnyAsync(d => d.Id == id));
    }

    // ---------- Photos ----------

    [Fact]
    public async Task At_least_one_photo_is_required()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db)
            .CreateAsync(TestDatabase.DonatorId, _database.ValidInput(), Array.Empty<IFormFile>());

        Assert.Equal(UiText.Validation.ImagesRequired, result.Message);
    }

    [Fact]
    public async Task More_than_three_photos_are_rejected()
    {
        // The form is a single multi-select field, so this is the server-side backstop for a
        // user who selected four files at once.
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db)
            .CreateAsync(TestDatabase.DonatorId, _database.ValidInput(), TestDatabase.Images(4));

        Assert.Equal(UiText.Validation.TooManyImages, result.Message);
        Assert.False(await db.DonationItems.AnyAsync());
    }

    [Fact]
    public async Task Three_photos_are_stored_in_order()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db)
            .CreateAsync(TestDatabase.DonatorId, _database.ValidInput(), TestDatabase.Images(3));

        Assert.True(result.Succeeded, result.Message);

        var images = await db.DonationImages.AsNoTracking()
            .Where(i => i.DonationItemId == result.Value)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();

        Assert.Equal(3, images.Count);
        Assert.Equal(new[] { 0, 1, 2 }, images.Select(i => i.SortOrder));
    }

    // ---------- Location ----------

    [Fact]
    public async Task A_neighborhood_from_another_city_is_rejected()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput(cityId: _database.ClujId, neighborhoodId: _database.BrasovCentruId),
            TestDatabase.OneImage());

        Assert.Equal(UiText.Validation.NeighborhoodNotInCity, result.Message);
    }

    [Fact]
    public async Task Food_can_be_published_without_a_neighborhood()
    {
        await using var db = _database.CreateContext();

        var result = await _database.CreateDonationService(db).CreateAsync(
            TestDatabase.DonatorId,
            _database.ValidInput() with { NeighborhoodId = null },
            TestDatabase.OneImage());

        Assert.True(result.Succeeded, result.Message);
    }

    // ---------- Editing ----------

    [Fact]
    public async Task Owner_can_edit_an_available_listing()
    {
        var id = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db).UpdateAsync(
            TestDatabase.DonatorId, id, _database.ValidInput("Covrigi ambalați"), Array.Empty<IFormFile>(), Array.Empty<int>());

        Assert.True(result.Succeeded, result.Message);

        var stored = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal("Covrigi ambalați", stored.Title);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Another_user_cannot_edit_someone_elses_listing()
    {
        var id = await _database.CreateDonationAsync(TestDatabase.DonatorId);

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db).UpdateAsync(
            TestDatabase.OtherDonatorId, id, _database.ValidInput("Modificat"), Array.Empty<IFormFile>(), Array.Empty<int>());

        Assert.Equal(ServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task Removing_the_last_photo_is_rejected()
    {
        var id = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var imageId = await db.DonationImages.Where(i => i.DonationItemId == id).Select(i => i.Id).SingleAsync();

        var result = await _database.CreateDonationService(db).UpdateAsync(
            TestDatabase.DonatorId, id, _database.ValidInput(), Array.Empty<IFormFile>(), new[] { imageId });

        Assert.Equal(UiText.Validation.ImagesRequired, result.Message);
        Assert.True(await db.DonationImages.AnyAsync(i => i.Id == imageId));
    }

    [Fact]
    public async Task Reserved_listing_cannot_be_edited()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db).UpdateAsync(
            TestDatabase.DonatorId, donationId, _database.ValidInput("Modificat"), Array.Empty<IFormFile>(), Array.Empty<int>());

        Assert.Equal(ServiceError.InvalidState, result.Error);
        Assert.Equal(UiText.Errors.EditNotAllowed, result.Message);
    }

    [Fact]
    public async Task Cancelled_listing_is_archived_and_hidden()
    {
        var id = await _database.CreateDonationAsync();

        await using (var db = _database.CreateContext())
        {
            Assert.True((await _database.CreateDonationService(db).CancelAsync(id, TestDatabase.DonatorId)).Succeeded);
        }

        await using var check = _database.CreateContext();
        var service = _database.CreateDonationService(check);

        var stored = await check.DonationItems.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal(DonationStatus.Cancelled, stored.Status);
        Assert.NotNull(stored.CancelledAt);

        var feed = await service.SearchFeedAsync(_database.Feed(), TestDatabase.ReceiverId);
        Assert.DoesNotContain(feed.Items, i => i.Id == id);
    }
}
