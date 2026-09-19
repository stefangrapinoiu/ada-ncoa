using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Services;

/// <summary>Validated values used to create or update a donation.</summary>
public sealed record DonationInput(
    string Title,
    string Description,
    int CategoryId,
    ProductCondition Condition,
    string PickupArea);

public enum DonationSort
{
    Newest = 0,
    Oldest = 1
}

public sealed class DonationSearchQuery
{
    public const int DefaultPageSize = 12;

    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public ProductCondition? Condition { get; set; }

    public string? Area { get; set; }

    public DonationSort Sort { get; set; } = DonationSort.Newest;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;
}

public sealed record DonationCard(
    int Id,
    string Title,
    string CategoryName,
    ProductCondition Condition,
    string PickupArea,
    DonationStatus Status,
    string? ImagePath,
    DateTime CreatedAt);

public sealed record DonatorDashboard(
    string FirstName,
    int AvailableCount,
    int ReservedCount,
    int CompletedCount,
    IReadOnlyList<DonatorActiveItem> ActiveItems);

public sealed record DonatorActiveItem(
    int Id,
    string Title,
    string CategoryName,
    DonationStatus Status,
    string? ImagePath,
    DateTime CreatedAt,
    int? ActiveReservationId,
    string? ReceiverFirstName);

public sealed record DonatorHistoryItem(
    int Id,
    string Title,
    string CategoryName,
    DonationStatus Status,
    string? ReceiverName,
    DateTime? ReservedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    DateTime CreatedAt);

public sealed record ReceiverReservationItem(
    int ReservationId,
    int DonationId,
    string Title,
    string CategoryName,
    string? ImagePath,
    string DonatorFirstName,
    DateTime ReservedAt,
    string? MeetingLocation,
    DateTime? MeetingAt,
    DateTime? CompletedAt);

public sealed record PickupDetailsInput(
    string MeetingLocation,
    DateTime MeetingAtUtc,
    string? Notes);
