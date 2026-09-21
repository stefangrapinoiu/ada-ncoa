using Microsoft.AspNetCore.Identity;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>Romanian messages for ASP.NET Core Identity errors.</summary>
public class RomanianIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };

    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "A apărut o eroare necunoscută.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Datele au fost modificate între timp. Reîncarcă pagina și încearcă din nou.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Parola este incorectă.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "Codul de securitate nu este valid.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Error(nameof(RecoveryCodeRedemptionFailed), "Codul de recuperare nu este valid.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "Există deja un cont asociat acestei autentificări.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), "Numele de utilizator nu este valid.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), "Adresa de e-mail nu este validă.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), "Există deja un cont cu această adresă de e-mail.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), "Există deja un cont cu această adresă de e-mail.");

    public override IdentityError InvalidRoleName(string? role) =>
        Error(nameof(InvalidRoleName), "Rolul nu este valid.");

    public override IdentityError DuplicateRoleName(string role) =>
        Error(nameof(DuplicateRoleName), "Rolul există deja.");

    public override IdentityError UserAlreadyHasPassword() =>
        Error(nameof(UserAlreadyHasPassword), "Contul are deja o parolă setată.");

    public override IdentityError UserLockoutNotEnabled() =>
        Error(nameof(UserLockoutNotEnabled), "Blocarea nu este activată pentru acest cont.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), "Contul are deja acest rol.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), "Contul nu are acest rol.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Parola trebuie să conțină cel puțin {length} caractere.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"Parola trebuie să conțină cel puțin {uniqueChars} caractere diferite.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Parola trebuie să conțină cel puțin un caracter special (ex: ! @ # .).");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Parola trebuie să conțină cel puțin o cifră.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Parola trebuie să conțină cel puțin o literă mică.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Parola trebuie să conțină cel puțin o literă mare.");
}
