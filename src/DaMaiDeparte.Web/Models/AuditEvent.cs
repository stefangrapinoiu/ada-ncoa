namespace DaMaiDeparte.Web.Models;

/// <summary>Security-relevant things that happen to accounts. Stored as strings.</summary>
public enum AuditEventType
{
    LoginSucceeded,
    LoginFailed,
    LockedOut,
    Logout,
    Registered,
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    TermsAccepted,
    AdminAccessDenied
}

/// <summary>Rough device class derived from the User-Agent header.</summary>
public enum DeviceType
{
    Unknown,
    Mobile,
    Tablet,
    Desktop
}

/// <summary>
/// One row of the security log shown in the admin panel ("Siguranță"). Holds personal data
/// (e-mail, IP), so it is only visible to admins and is meant to be purged after 90 days.
/// No foreign key to the user: the log must survive an account being deleted.
/// </summary>
public class AuditEvent
{
    public long Id { get; set; }

    public DateTime OccurredAt { get; set; }

    public AuditEventType Type { get; set; }

    public string? UserId { get; set; }

    /// <summary>The e-mail as typed (trimmed) — also set for failed logins to unknown accounts.</summary>
    public string? Email { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DeviceType DeviceType { get; set; }

    public string? Details { get; set; }
}
