namespace DaMaiDeparte.Web.Models;

/// <summary>
/// Lifecycle of a food listing. There is a single user type, so the status describes the
/// item, never the account.
/// </summary>
public enum DonationStatus
{
    Available = 1,
    Reserved = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5
}

/// <summary>Status filter shown above the dashboard feed.</summary>
public enum FeedFilter
{
    /// <summary>Available + Reserved (default).</summary>
    All = 0,
    Available = 1,
    Reserved = 2
}
