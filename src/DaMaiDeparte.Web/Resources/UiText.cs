using DaMaiDeparte.Web.Infrastructure;

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
        public const string TitleRequired = "Denumirea alimentului este obligatorie.";
        public const string TitleLength = "Denumirea trebuie să conțină între 3 și 100 de caractere.";
        public const string CategoryRequired = "Selectează o categorie de alimente.";
        public const string InvalidCategory = "Categoria selectată nu este disponibilă pentru donații.";
        public const string CategoryNotAllowed = "Această categorie de alimente nu poate fi donată pe platformă.";

        public const string ExpirationRequired = "Data expirării este obligatorie.";
        public const string ExpirationInvalid = "Introdu o dată validă, în formatul zz/ll/aaaa.";
        public const string DateInvalid = "Introdu o dată validă, în formatul zz/ll/aaaa.";
        public const string TimeInvalid = "Introdu o oră validă, în formatul hh:mm (de exemplu 18:30).";
        public const string Expired = "Acest aliment este expirat și nu poate fi publicat.";
        public const string ExpiresTooSoon = "Alimentul trebuie să mai fie valabil cel puțin 3 zile pentru a putea fi publicat.";
        public const string ExpirationTooFar = "Verifică data expirării: pare prea îndepărtată.";

        public const string CountryRequired = "Selectează țara.";
        public const string CountyRequired = "Selectează județul.";
        public const string LocalityRequired = "Selectează localitatea.";
        public const string InvalidLocation = "Locația selectată nu este validă.";
        public const string NeighborhoodNotInCity = "Cartierul selectat nu aparține localității alese.";

        public const string ImagesRequired = "Adaugă cel puțin o fotografie a alimentului.";
        public const string TooManyImages = "Poți adăuga cel mult 3 fotografii.";
        public const string ImageFormat = "Formatul imaginii nu este acceptat. Folosește JPG, PNG sau WebP.";
        public const string ImageTooLarge = "Imaginea selectată este prea mare.";
        public const string ImageInvalid = "Fișierul selectat nu este o imagine validă.";

        public const string SafetyPackagingRequired = "Confirmă că alimentul este ambalat, sigilat și în ambalajul original.";
        public const string SafetyNoMeatDairyRequired = "Confirmă că alimentul nu conține carne, pește sau produse lactate.";
        public const string SafetyExpiryVisibleRequired = "Confirmă că data expirării este vizibilă pe ambalaj.";

        public const string EmailRequired = "Adresa de e-mail este obligatorie.";
        public const string EmailInvalid = "Introdu o adresă de e-mail validă.";
        public const string PasswordRequired = "Parola este obligatorie.";
        public const string PasswordLength = "Parola trebuie să conțină cel puțin {2} caractere.";
        public const string PasswordsDoNotMatch = "Parolele introduse nu coincid.";
        public const string FirstNameRequired = "Prenumele este obligatoriu.";
        public const string LastNameRequired = "Numele este obligatoriu.";
        public const string NameLength = "Acest câmp nu poate depăși {1} caractere.";
        public const string PhoneInvalid = "Introdu un număr de telefon valid.";

        public const string PickupLocationRequired = "Locul predării este obligatoriu.";
        public const string PickupLocationLength = "Locul predării nu poate depăși 200 de caractere.";
        public const string PickupNotesLength = "Detaliile predării nu pot depăși 500 de caractere.";
        public const string MeetingLocationRequired = "Locul predării este obligatoriu.";
        public const string MeetingLocationLength = "Locul predării nu poate depăși 200 de caractere.";
        public const string MeetingDateRequired = "Data predării este obligatorie.";
        public const string MeetingTimeRequired = "Ora predării este obligatorie.";
        public const string MeetingAtRequired = "Data și ora predării sunt obligatorii.";
        public const string MeetingAtInPast = "Data și ora predării trebuie să fie în viitor.";
        public const string NotesLength = "Instrucțiunile nu pot depăși 500 de caractere.";

        public const string MessageBodyRequired = "Scrie un mesaj înainte de a-l trimite.";
        public const string MessageBodyLength = "Mesajul nu poate depăși 1000 de caractere.";
    }

    public static class Success
    {
        public const string DonationPublished = "Alimentul a fost publicat cu succes.";
        public const string DonationUpdated = "Modificările au fost salvate.";
        public const string DonationCancelled = "Donația a fost anulată.";
        public const string DonationCompleted = "Donația a fost finalizată. Mulțumim că ai salvat acest aliment de la risipă!";
        public const string ReservationCreated = "Alimentul a fost rezervat cu succes.";
        public const string ReservationCancelled = "Rezervarea a fost anulată.";
        public const string ReservationReleased = "Rezervarea a fost anulată, iar alimentul este din nou disponibil.";
        public const string PickupSaved = "Detaliile predării au fost salvate.";
        public const string LocationChanged = "Locația a fost actualizată.";
        public const string DefaultLocationSaved = "Locația a fost salvată în profilul tău.";
        public const string AccountCreated = "Contul a fost creat cu succes.";
        public const string AccountUpdated = "Datele contului au fost actualizate.";
        public const string PasswordChanged = "Parola a fost schimbată.";
    }

    public static class Errors
    {
        public const string Generic = "Nu am putut finaliza operațiunea. Te rugăm să încerci din nou.";
        public const string ReservationConflict = "Ne pare rău, acest aliment tocmai a fost rezervat de altcineva.";
        public const string NotAvailableForReservation = "Acest aliment nu mai este disponibil pentru rezervare.";
        public const string CannotReserveOwnDonation = "Nu poți rezerva propria donație.";
        public const string DonationExpired = "Acest aliment a expirat și nu mai poate fi rezervat.";
        public const string NotOwner = "Nu ai permisiunea să modifici această donație.";
        public const string EditNotAllowed = "Acest aliment nu mai poate fi modificat deoarece a fost deja rezervat.";
        public const string CancelNotAllowed = "Această donație nu mai poate fi anulată.";
        public const string CompleteNotAllowed = "Donația poate fi finalizată doar după ce alimentul a fost rezervat.";
        public const string ReservationNotFound = "Rezervarea nu a fost găsită.";
        public const string ReservationNotActive = "Această rezervare nu mai este activă.";
        public const string DonationNotFound = "Alimentul nu a fost găsit.";
        public const string ConcurrentUpdate = "Alimentul a fost modificat între timp. Reîncarcă pagina și încearcă din nou.";
        public const string InvalidLogin = "Adresa de e-mail sau parola nu sunt corecte.";
        public const string LockedOut = "Contul a fost blocat temporar din cauza prea multor încercări. Încearcă din nou mai târziu.";
        public const string ImageSaveFailed = "Imaginea nu a putut fi salvată. Încearcă din nou.";
        public const string NoLocationSelected = "Alege mai întâi locația în care vrei să cauți.";
        public const string ProhibitedMeat = "Pe această platformă nu pot fi donate carne, mezeluri, pește sau fructe de mare.";
        public const string ProhibitedDairy = "Pe această platformă nu pot fi donate lapte, iaurt, brânză, smântână, unt sau alte produse lactate.";
    }

    public static class Status
    {
        public const string Available = "Disponibil";
        public const string Reserved = "Rezervat";
        public const string Completed = "Donat";
        public const string Cancelled = "Anulat";
        public const string Expired = "Expirat";

        public const string AvailableFeminine = "Disponibilă";
        public const string ReservedFeminine = "Rezervată";
        public const string CompletedFeminine = "Donată";
        public const string CancelledFeminine = "Anulată";
        public const string ExpiredFeminine = "Expirată";
    }

    public static class Location
    {
        public const string Country = "Țara";
        public const string County = "Județul";
        public const string Locality = "Localitatea";
        public const string Neighborhood = "Cartierul";
        public const string NeighborhoodHelp = "Opțional. Te ajută să găsești alimente mai aproape de tine.";
        public const string AllNeighborhoods = "Toate cartierele";
        public const string NoNeighborhood = "Fără cartier";
        public const string WhereDoYouSearch = "Unde vrei să cauți?";
        public const string YourLocation = "Locația ta";
        public const string Change = "Schimbă locația";
        public const string Apply = "Aplică";
        public const string Cancel = "Anulează";
        public const string SeeFood = "Vezi alimentele";
        public const string MyLocation = "Locația mea";
        public const string SettingsHint = "Locația se stabilește la crearea contului și poate fi schimbată oricând din setări.";
        public const string Save = "Salvează locația";
    }

    public static class Food
    {
        public const string OnlyFood = "Platforma este destinată exclusiv alimentelor.";
        public const string PackagedOnly = "În această versiune, pot fi donate doar alimente ambalate, sigilate și aflate în ambalajul original.";
        public const string NoMeatNoDairy = "Nu sunt permise carnea, peștele, fructele de mare și produsele lactate.";
        public const string ExpirationLabel = "Data expirării";
        public const string ExpirationHelp = "Data trebuie să fie vizibilă și pe ambalajul produsului.";
        public const string PhotosHelp = "Poți selecta până la 3 fotografii dintr-o singură dată. Asigură-te că produsul și data expirării sunt vizibile.";
        public const string PickupTitle = "Detaliile predării";
        public const string PickupLocationLabel = "Locul predării";
        public const string PickupLocationHelp = "Alege un loc public. Nu publica adresa exactă de acasă.";
        public const string PickupNotesLabel = "Detalii despre predare";
        public const string PickupNotesHelp = "Opțional. Ex: „zilnic după ora 18:00” sau „doar în weekend”.";

        public const string ConfirmPackaging = "Confirm că alimentul este ambalat, sigilat și se află în ambalajul original.";
        public const string ConfirmNoMeatDairy = "Confirm că alimentul nu conține carne, pește, fructe de mare sau produse lactate.";
        public const string ConfirmExpiryVisible = "Confirm că data expirării este vizibilă pe ambalaj și în fotografii.";

        public static readonly string MinimumShelfLife =
            $"Alimentul trebuie să mai fie valabil cel puțin {FoodRules.MinimumShelfLifeDays} zile.";
    }

    public static class Confirm
    {
        public const string CancelDonation = "Ești sigur că vrei să anulezi această donație? Alimentul nu va mai apărea în listă.";
        public const string CancelDonationButton = "Da, anulează donația";
        public const string CancelReservation = "Ești sigur că vrei să anulezi această rezervare? Alimentul va deveni din nou disponibil pentru alte persoane.";
        public const string CancelReservationButton = "Da, anulează rezervarea";
        public const string ReleaseReservation = "Ești sigur că vrei să anulezi această rezervare? Folosește această opțiune dacă persoana care a rezervat nu mai răspunde. Alimentul va deveni din nou disponibil pentru alte persoane.";
        public const string ReleaseReservationButton = "Da, anulează rezervarea";
        public const string CompleteDonation = "Confirmi că alimentul a fost predat? După confirmare, donația va fi finalizată și nu va mai apărea în lista alimentelor disponibile.";
        public const string CompleteDonationButton = "Da, finalizează donația";
        public const string Back = "Înapoi";
    }

    public const string ImageUnavailable = "Imagine indisponibilă";

    public static string ImageAlt(string title) => $"Fotografie pentru alimentul „{title}”";

    public static string ImageAlt(string title, int index) => $"Fotografia {index} pentru alimentul „{title}”";
}
