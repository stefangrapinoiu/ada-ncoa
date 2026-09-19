namespace DaMaiDeparte.Web.Infrastructure;

public sealed record EmptyStateModel(
    string Heading,
    string? Text = null,
    string? ButtonText = null,
    string? ButtonUrl = null,
    string Icon = "bi-box-seam");

public sealed record DonationImageModel(string? ImagePath, string Title, string CssClass);
