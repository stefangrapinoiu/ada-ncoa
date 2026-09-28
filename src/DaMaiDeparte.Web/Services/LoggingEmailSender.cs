namespace DaMaiDeparte.Web.Services;

/// <summary>
/// Fallback "sender" used while no SMTP server is configured (Email:Host is empty — the
/// default, since no real credentials exist in this repo yet). Instead of sending, it logs
/// the e-mail so a password-reset link can still be found and used locally, e.g. via
/// `docker compose logs`. See Program.cs for how this is selected instead of SmtpEmailSender.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email:Host is not configured, so no e-mail was actually sent. Would have sent to {Recipient}, subject '{Subject}':\n{Body}",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
