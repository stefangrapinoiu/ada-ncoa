using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;

namespace DaMaiDeparte.Tests;

public class HelperTests
{
    [Theory]
    [InlineData(DonationStatus.Available, "Disponibil")]
    [InlineData(DonationStatus.Reserved, "Rezervat")]
    [InlineData(DonationStatus.Completed, "Donat")]
    [InlineData(DonationStatus.Cancelled, "Anulat")]
    [InlineData(DonationStatus.Expired, "Expirat")]
    public void Status_labels_are_romanian(DonationStatus status, string expected) =>
        Assert.Equal(expected, status.ToLabel());

    [Fact]
    public void Dates_are_formatted_day_first_in_romanian_local_time()
    {
        // 15:30 UTC in September = 18:30 in Bucharest (EEST, UTC+3).
        var utc = new DateTime(2026, 9, 15, 15, 30, 0, DateTimeKind.Utc);

        Assert.Equal("15/09/2026", RoDate.FormatDate(utc));
        Assert.Equal("15/09/2026, 18:30", RoDate.FormatDateTime(utc));
    }

    [Fact]
    public void A_date_whose_day_and_month_could_be_swapped_is_unambiguous()
    {
        // 3 February, not 2 March: day first, never the US month-first order.
        Assert.Equal("03/02/2026", RoDate.FormatDate(new DateOnly(2026, 2, 3)));
    }

    [Fact]
    public void Form_values_are_day_first()
    {
        Assert.Equal("03/02/2026", RoDate.ToDateInput(new DateOnly(2026, 2, 3)));
        Assert.Equal(string.Empty, RoDate.ToDateInput((DateOnly?)null));
        Assert.Equal("18:30", RoDate.ToTimeInput(new DateTime(2026, 9, 15, 15, 30, 0, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData("03/02/2026")]
    [InlineData("3/2/2026")]
    [InlineData("03.02.2026")]
    [InlineData("03-02-2026")]
    [InlineData("2026-02-03")]   // ISO, in case a browser autofills one
    public void Typed_dates_are_read_day_first(string typed) =>
        Assert.Equal(new DateOnly(2026, 2, 3), RoDate.ParseDate(typed));

    [Theory]
    [InlineData("13/13/2026")]   // no 13th month
    [InlineData("02/03")]        // incomplete
    [InlineData("not a date")]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_dates_are_rejected(string? typed) => Assert.Null(RoDate.ParseDate(typed));

    [Theory]
    [InlineData("18:30")]
    [InlineData("18.30")]
    [InlineData("1830")]
    public void Typed_times_are_read_as_24_hour(string typed) =>
        Assert.Equal(new TimeOnly(18, 30), RoDate.ParseTime(typed));

    [Theory]
    [InlineData("25:00")]
    [InlineData("6:30 PM")]
    [InlineData("")]
    public void Invalid_times_are_rejected(string typed) => Assert.Null(RoDate.ParseTime(typed));

    [Fact]
    public void Local_input_is_converted_to_utc()
    {
        var local = new DateTime(2026, 9, 15, 18, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateTime(2026, 9, 15, 15, 30, 0, DateTimeKind.Utc), RoDate.LocalToUtc(local));
    }

    [Fact]
    public void Expiry_dates_are_calendar_dates_not_utc_days()
    {
        // 22:30 UTC on 15 September is already 16 September in Bucharest. Using UtcNow.Date
        // here would shift every expiry rule by a day for part of each evening.
        var lateEvening = new DateTime(2026, 9, 15, 22, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 9, 16), RoDate.TodayAt(lateEvening));
        Assert.NotEqual(DateOnly.FromDateTime(lateEvening), RoDate.TodayAt(lateEvening));
    }

    [Fact]
    public void Earliest_allowed_expiry_is_three_days_out()
    {
        Assert.Equal(RoDate.Today.AddDays(3), RoDate.EarliestAllowedExpiry);
        Assert.Equal(3, FoodRules.MinimumShelfLifeDays);
    }

    [Theory]
    [InlineData("expirat", -1)]
    [InlineData("expiră azi", 0)]
    [InlineData("expiră mâine", 1)]
    [InlineData("mai are 5 zile", 5)]
    public void Remaining_shelf_life_is_described_in_romanian(string expected, int daysAhead) =>
        Assert.Equal(expected, RoDate.DescribeRemaining(RoDate.Today.AddDays(daysAhead)));

    // ---------- Prohibited-food screen ----------

    [Theory]
    [InlineData("Salam de Sibiu")]
    [InlineData("SALAM")]                 // case-insensitive
    [InlineData("Șuncă presată")]         // diacritics
    [InlineData("Sunca presata")]         // diacritic-free spelling of the same word
    [InlineData("Mezeluri asortate")]
    [InlineData("Conservă de ton")]
    [InlineData("Fructe de mare congelate")]
    public void Meat_terms_are_detected(string text) =>
        Assert.Equal(ProhibitedFoodGroup.Meat, ProhibitedFoodScreen.Detect(text));

    [Theory]
    [InlineData("Iaurt grecesc")]
    [InlineData("Cașcaval afumat")]
    [InlineData("Lapte UHT")]
    [InlineData("Unt 82%")]
    [InlineData("Smântână pentru gătit")]
    public void Dairy_terms_are_detected(string text) =>
        Assert.Equal(ProhibitedFoodGroup.Dairy, ProhibitedFoodScreen.Detect(text));

    [Theory]
    [InlineData("Unt de arahide")]        // plant-based, allowed
    [InlineData("Lapte de migdale")]
    [InlineData("Pâine integrală feliată")]
    [InlineData("Paste penne integrale")]
    [InlineData("Casa de marcat")]        // "cas" must not fire on "casa"
    public void Allowed_food_is_not_flagged(string text) =>
        Assert.Equal(ProhibitedFoodGroup.None, ProhibitedFoodScreen.Detect(text));

    [Fact]
    public void Screen_looks_at_the_handover_notes_too() =>
        Assert.Equal(ProhibitedFoodGroup.Meat, ProhibitedFoodScreen.Detect("Pachet asortat", "Aduc și niște pastramă."));

    [Fact]
    public void Normalization_strips_diacritics_and_punctuation() =>
        Assert.Equal("paine integrala feliata", ProhibitedFoodScreen.Normalize("Pâine   integrală, feliată!"));

    [Fact]
    public void Image_signatures_are_detected()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        byte[] webp = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];
        byte[] fake = "<?php echo 1;"u8.ToArray();

        Assert.Equal(".jpg", LocalFileStorageService.DetectExtension(jpeg));
        Assert.Equal(".png", LocalFileStorageService.DetectExtension(png));
        Assert.Equal(".webp", LocalFileStorageService.DetectExtension(webp));
        Assert.Null(LocalFileStorageService.DetectExtension(fake));
    }
}
