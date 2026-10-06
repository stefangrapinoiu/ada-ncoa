using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Monitoring;

/// <summary>
/// The single write path for the security log. Never throws: a failed audit write must not
/// break the user's action (login, registration, ...).
/// </summary>
public interface IAuditLog
{
    Task WriteAsync(AuditEventType type, string? userId, string? email, string? details = null, CancellationToken cancellationToken = default);
}
