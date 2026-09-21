namespace DaMaiDeparte.Web.Infrastructure;

public sealed record EmptyStateModel(
    string Heading,
    string? Text = null,
    string? ButtonText = null,
    string? ButtonUrl = null,
    string Icon = "bi-box-seam");

public sealed record DonationImageModel(string? ImagePath, string Title, string CssClass);

/// <summary>Header strip showing the active browsing location plus a "Schimbă locația" link.</summary>
public sealed record LocationBarModel(BrowsingLocation Location, string? ReturnUrl = null);
