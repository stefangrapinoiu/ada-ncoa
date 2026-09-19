namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>Romanian empty-state content used across pages.</summary>
public static class EmptyStates
{
    public static readonly EmptyStateModel DonatorNoProducts = new(
        "Nu ai publicat încă niciun produs",
        "Ai ceva ce nu mai folosești? Publică primul produs și ajută-l să își găsească un nou proprietar.",
        "Adaugă un produs",
        "/Donator/Donations/Create",
        "bi-box-seam");

    public static readonly EmptyStateModel DonatorNoProductsInSection = new(
        "Nu ai produse în această secțiune",
        Icon: "bi-inbox");

    public static readonly EmptyStateModel ReceiverNoActiveReservations = new(
        "Nu ai rezervări active",
        "Descoperă produsele disponibile și găsește ceva care îți poate fi util.",
        "Vezi produse",
        "/Donations",
        "bi-bag-heart");

    public static readonly EmptyStateModel DonatorNoCompleted = new(
        "Nu ai încă donații finalizate.",
        Icon: "bi-archive");

    public static readonly EmptyStateModel DonatorNoCancelled = new(
        "Nu ai donații anulate.",
        Icon: "bi-archive");

    public static readonly EmptyStateModel ReceiverNoReceived = new(
        "Nu ai primit încă niciun produs.",
        null,
        "Vezi produse disponibile",
        "/Donations",
        "bi-gift");

    public static readonly EmptyStateModel SearchNoResults = new(
        "Nu am găsit rezultate",
        "Încearcă să modifici termenul de căutare sau filtrele selectate.",
        "Resetează filtrele",
        "/Donations",
        "bi-search");

    public static readonly EmptyStateModel NoDonationsYet = new(
        "Momentan nu există produse disponibile",
        "Revino în curând – comunitatea adaugă mereu produse noi.",
        Icon: "bi-hourglass-split");
}
