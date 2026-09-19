namespace DaMaiDeparte.Web.Resources;

/// <summary>
/// Centralized Romanian UI strings shared across pages and services.
/// Page-specific copy lives in the Razor views. To add another language later,
/// these constants can be moved to SharedResource.{culture}.resx files and read via IStringLocalizer.
/// </summary>
public static class UiText
{
    public static class Validation
    {
        public const string Required = "Acest câmp este obligatoriu.";
        public const string TitleRequired = "Titlul este obligatoriu.";
        public const string TitleLength = "Titlul trebuie să conțină între 3 și 100 de caractere.";
        public const string DescriptionRequired = "Descrierea este obligatorie.";
        public const string DescriptionLength = "Descrierea nu poate depăși 2000 de caractere.";
        public const string CategoryRequired = "Selectează o categorie.";
        public const string ConditionRequired = "Selectează starea produsului.";
        public const string PickupAreaRequired = "Zona de predare este obligatorie.";
        public const string PickupAreaLength = "Zona de predare nu poate depăși 100 de caractere.";
        public const string EmailRequired = "Adresa de e-mail este obligatorie.";
        public const string EmailInvalid = "Introdu o adresă de e-mail validă.";
        public const string PasswordRequired = "Parola este obligatorie.";
        public const string PasswordLength = "Parola trebuie să conțină cel puțin {2} caractere.";
        public const string PasswordsDoNotMatch = "Parolele introduse nu coincid.";
        public const string FirstNameRequired = "Prenumele este obligatoriu.";
        public const string LastNameRequired = "Numele este obligatoriu.";
        public const string NameLength = "Acest câmp nu poate depăși {1} caractere.";
        public const string PhoneInvalid = "Introdu un număr de telefon valid.";
        public const string AccountTypeRequired = "Alege cum dorești să folosești aplicația.";
        public const string MeetingLocationRequired = "Locul predării este obligatoriu.";
        public const string MeetingLocationLength = "Locul predării nu poate depăși 200 de caractere.";
        public const string MeetingAtRequired = "Data și ora predării sunt obligatorii.";
        public const string MeetingAtInPast = "Data și ora predării trebuie să fie în viitor.";
        public const string NotesLength = "Instrucțiunile nu pot depăși 500 de caractere.";
        public const string ImageFormat = "Formatul imaginii nu este acceptat. Folosește JPG, PNG sau WebP.";
        public const string ImageTooLarge = "Imaginea selectată este prea mare.";
        public const string ImageInvalid = "Fișierul selectat nu este o imagine validă.";
        public const string InvalidCategory = "Categoria selectată nu există.";
        public const string InvalidCondition = "Starea selectată nu este validă.";
    }

    public static class Success
    {
        public const string DonationPublished = "Produsul a fost publicat cu succes.";
        public const string DonationUpdated = "Modificările au fost salvate.";
        public const string ReservationCreated = "Produsul a fost rezervat cu succes.";
        public const string ReservationCancelled = "Rezervarea a fost anulată.";
        public const string PickupSaved = "Detaliile predării au fost salvate.";
        public const string DonationCompleted = "Donația a fost finalizată cu succes. Mulțumim că ai dat acestui produs o nouă viață!";
        public const string DonationCancelled = "Donația a fost anulată.";
        public const string AccountUpdated = "Datele contului au fost actualizate.";
        public const string PasswordChanged = "Parola a fost schimbată.";
    }

    public static class Errors
    {
        public const string Generic = "Nu am putut finaliza operațiunea. Te rugăm să încerci din nou.";
        public const string ReservationConflict = "Ne pare rău, acest produs tocmai a fost rezervat de altcineva.";
        public const string NotAvailableForReservation = "Acest produs nu mai este disponibil pentru rezervare.";
        public const string OnlyReceiversCanReserve = "Doar beneficiarii pot rezerva produse.";
        public const string OnlyDonatorsCanDonate = "Doar donatorii pot publica produse.";
        public const string NotOwner = "Nu ai permisiunea să modifici această donație.";
        public const string EditNotAllowed = "Acest produs nu mai poate fi modificat deoarece a fost deja rezervat.";
        public const string CancelNotAllowed = "Această donație nu mai poate fi anulată.";
        public const string CompleteNotAllowed = "Donația poate fi finalizată doar după ce produsul a fost rezervat.";
        public const string ReservationNotFound = "Rezervarea nu a fost găsită.";
        public const string ReservationNotActive = "Această rezervare nu mai este activă.";
        public const string DonationNotFound = "Produsul nu a fost găsit.";
        public const string ConcurrentUpdate = "Produsul a fost modificat între timp. Reîncarcă pagina și încearcă din nou.";
        public const string InvalidLogin = "Adresa de e-mail sau parola nu sunt corecte.";
        public const string LockedOut = "Contul a fost blocat temporar din cauza prea multor încercări. Încearcă din nou mai târziu.";
        public const string ImageSaveFailed = "Imaginea nu a putut fi salvată. Încearcă din nou.";
    }

    public static class Status
    {
        public const string Available = "Disponibil";
        public const string Reserved = "Rezervat";
        public const string Completed = "Finalizat";
        public const string Cancelled = "Anulat";

        public const string AvailableFeminine = "Disponibilă";
        public const string ReservedFeminine = "Rezervată";
        public const string CompletedFeminine = "Finalizată";
        public const string CancelledFeminine = "Anulată";
    }

    public static class Condition
    {
        public const string New = "Nou";
        public const string LikeNew = "Ca nou";
        public const string Good = "Stare bună";
        public const string Used = "Folosit";
        public const string NeedsRepair = "Necesită reparații";
    }

    public static class Confirm
    {
        public const string CancelDonation = "Ești sigur că vrei să anulezi această donație? Produsul nu va mai apărea în lista publică.";
        public const string CancelDonationButton = "Da, anulează donația";
        public const string CancelReservation = "Ești sigur că vrei să anulezi această rezervare? Produsul va deveni din nou disponibil pentru alte persoane.";
        public const string CancelReservationButton = "Da, anulează rezervarea";
        public const string CompleteDonation = "Confirmi că produsul a fost predat beneficiarului? După confirmare, donația va fi finalizată și nu va mai apărea în lista produselor disponibile.";
        public const string CompleteDonationButton = "Da, finalizează donația";
        public const string Back = "Înapoi";
    }

    public const string ImageUnavailable = "Imagine indisponibilă";

    public static string ImageAlt(string title) => $"Fotografie pentru produsul „{title}”";
}
