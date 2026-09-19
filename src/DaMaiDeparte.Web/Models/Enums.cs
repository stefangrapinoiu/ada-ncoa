namespace DaMaiDeparte.Web.Models;

public enum AccountType
{
    Donator = 1,
    Receiver = 2
}

public enum DonationStatus
{
    Available = 1,
    Reserved = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ProductCondition
{
    New = 1,
    LikeNew = 2,
    Good = 3,
    Used = 4,
    NeedsRepair = 5
}
