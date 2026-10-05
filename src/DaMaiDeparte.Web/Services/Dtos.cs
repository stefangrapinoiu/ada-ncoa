using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Services;

/// <summary>Validated values used to create or update a food listing.</summary>
public sealed record DonationInput(
    string Title,
    int FoodCategoryId,
    DateOnly ExpirationDate,
    int CountryId,
    int CityId,
    int? NeighborhoodId,
    string PickupLocation,
    string? PickupNotes);

/// <summary>
/// Everything the dashboard feed needs. The city is mandatory: "nearby" in Version 1 means
/// "the selected city", optionally narrowed to one neighborhood.
/// </summary>
public sealed class FeedQuery
{
    public const int DefaultPageSize = 12;

    public required int CityId { get; init; }

    public int? NeighborhoodId { get; init; }

    public FeedFilter Filter { get; init; } = FeedFilter.All;

    public int? FoodCategoryId { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}

public sealed record DonationCard(
    int Id,
    string Title,
    string CategoryName,
    DateOnly ExpirationDate,
    string CityName,
    string? NeighborhoodName,
    string PickupLocation,
    DonationStatus Status,
    string? ImagePath,
    int ImageCount,
    DateTime CreatedAt,
    bool IsOwn);

/// <summary>A listing owned by the current user, shown under "Donațiile mele".</summary>
public sealed record MyDonationItem(
    int Id,
    string Title,
    string CategoryName,
    DateOnly ExpirationDate,
    string CityName,
    string? NeighborhoodName,
    string PickupLocation,
    DonationStatus Status,
    string? ImagePath,
    DateTime CreatedAt,
    int? ActiveReservationId,
    string? ReceiverFirstName);

public sealed record UserSummary(
    string FirstName,
    int AvailableCount,
    int ReservedCount,
    int CompletedCount,
    int ActiveReservationCount);

public sealed record MyReservationItem(
    int ReservationId,
    int DonationId,
    string Title,
    string CategoryName,
    DateOnly ExpirationDate,
    string? ImagePath,
    string DonatorFirstName,
    string CityName,
    string? NeighborhoodName,
    string PickupLocation,
    string? PickupNotes,
    DateTime ReservedAt,
    string? MeetingLocation,
    DateTime? MeetingAt,
    DateTime? CompletedAt);

public sealed record PickupDetailsInput(
    string MeetingLocation,
    DateTime MeetingAtUtc,
    string? Notes);

/// <summary>One message in a reservation's in-app conversation, ready to render.</summary>
public sealed record ReservationMessageItem(
    int Id,
    string SenderFirstName,
    bool IsMine,
    string Body,
    DateTime CreatedAt);
