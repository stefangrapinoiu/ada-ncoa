namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>Romanian empty-state content used across pages.</summary>
public static class EmptyStates
{
    public static readonly EmptyStateModel NoFoodInCity = new(
        "Momentan nu există alimente disponibile aici",
        "Revino în curând sau încearcă alt oraș ori alt cartier. Poți fi tu primul care oferă ceva.",
        "Adaugă un aliment",
        "/Donations/Create",
        "bi-basket");

    public static readonly EmptyStateModel NoFoodForFilters = new(
        "Niciun aliment pentru filtrele alese",
        "Încearcă „Toate cartierele” sau schimbă filtrul de stare.",
        Icon: "bi-funnel");

    public static readonly EmptyStateModel NoDonationsYet = new(
        "Nu ai publicat încă niciun aliment",
        "Ai un produs ambalat pe care nu îl vei consuma? Oferă-l unei persoane din apropiere.",
        "Adaugă un aliment",
        "/Donations/Create",
        "bi-basket");

    public static readonly EmptyStateModel NoDonationsInSection = new(
        "Nu ai alimente în această secțiune",
        Icon: "bi-inbox");

    public static readonly EmptyStateModel NoActiveReservations = new(
        "Nu ai rezervări active",
        "Descoperă alimentele disponibile în orașul tău și rezervă ce îți este util.",
        "Vezi alimentele",
        "/Dashboard",
        "bi-bag-heart");

    public static readonly EmptyStateModel NoReceivedFood = new(
        "Nu ai primit încă niciun aliment.",
        null,
        "Vezi alimentele disponibile",
        "/Dashboard",
        "bi-basket");

    public static readonly EmptyStateModel NoCompletedDonations = new(
        "Nu ai încă donații finalizate.",
        Icon: "bi-archive");
}
