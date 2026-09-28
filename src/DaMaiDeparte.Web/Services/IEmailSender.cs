namespace DaMaiDeparte.Web.Services;

/// <summary>
/// Minimal transactional e-mail abstraction. Used today only for password-reset links.
/// See EmailOptions / SmtpEmailSender / LoggingEmailSender and Program.cs for how the
/// implementation is chosen.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
