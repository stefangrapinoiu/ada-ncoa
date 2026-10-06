namespace DaMaiDeparte.Web.Services;

/// <summary>
/// Bound from the "Email" configuration section. No SMTP credentials exist anywhere in this
/// repo yet, so by default Email:Host is empty and Program.cs registers LoggingEmailSender
/// instead of SmtpEmailSender — see Program.cs for the switch. To send real e-mail, set
/// Email:Host (and Email:Username / Email:Password via user secrets or environment
/// variables — never commit real credentials to appsettings*.json).
/// </summary>
public sealed class EmailOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@ada-ncoa.ro";

    public string FromName { get; set; } = "Adă-nCoa";
}
