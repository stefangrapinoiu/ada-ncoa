using DaMaiDeparte.Web.Services;

namespace DaMaiDeparte.Web.Pages.Donator;

public sealed record DonatorItemListModel(IReadOnlyList<DonatorActiveItem> Items, string? ReturnUrl);
