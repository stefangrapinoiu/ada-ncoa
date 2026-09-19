using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;

namespace DaMaiDeparte.Tests;

public class HelperTests
{
    [Theory]
    [InlineData(DonationStatus.Available, "Disponibil")]
    [InlineData(DonationStatus.Reserved, "Rezervat")]
    [InlineData(DonationStatus.Completed, "Finalizat")]
    [InlineData(DonationStatus.Cancelled, "Anulat")]
    public void Status_labels_are_romanian(DonationStatus status, string expected) =>
        Assert.Equal(expected, status.ToLabel());

    [Theory]
    [InlineData(ProductCondition.New, "Nou")]
    [InlineData(ProductCondition.LikeNew, "Ca nou")]
    [InlineData(ProductCondition.Good, "Stare bună")]
    [InlineData(ProductCondition.Used, "Folosit")]
    [InlineData(ProductCondition.NeedsRepair, "Necesită reparații")]
    public void Condition_labels_are_romanian(ProductCondition condition, string expected) =>
        Assert.Equal(expected, condition.ToLabel());

    [Fact]
    public void Dates_are_formatted_in_romanian_local_time()
    {
        // 15:30 UTC in September = 18:30 in Bucharest (EEST, UTC+3).
        var utc = new DateTime(2026, 9, 15, 15, 30, 0, DateTimeKind.Utc);

        Assert.Equal("15 septembrie 2026", RoDate.FormatDate(utc));
        Assert.Equal("15 septembrie 2026, 18:30", RoDate.FormatDateTime(utc));
    }

    [Fact]
    public void Local_input_is_converted_to_utc()
    {
        var local = new DateTime(2026, 9, 15, 18, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateTime(2026, 9, 15, 15, 30, 0, DateTimeKind.Utc), RoDate.LocalToUtc(local));
    }

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
