using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Monitoring;

/// <summary>Romanian label, badge style and icon for each security-log event type.</summary>
public static class AuditLabels
{
    public static string Label(AuditEventType type) => type switch
    {
        AuditEventType.LoginSucceeded => "Autentificare",
        AuditEventType.LoginFailed => "Autentificare eșuată",
        AuditEventType.LockedOut => "Cont blocat",
        AuditEventType.Logout => "Deconectare",
        AuditEventType.Registered => "Cont nou",
        AuditEventType.PasswordChanged => "Parolă schimbată",
        AuditEventType.PasswordResetRequested => "Cerere resetare parolă",
        AuditEventType.PasswordResetCompleted => "Parolă resetată",
        AuditEventType.TermsAccepted => "Termeni acceptați",
        AuditEventType.AdminAccessDenied => "Acces admin refuzat",
        _ => type.ToString()
    };

    public static string Badge(AuditEventType type) => type switch
    {
        AuditEventType.LoginSucceeded or AuditEventType.Registered => "text-bg-success",
        AuditEventType.LoginFailed or AuditEventType.LockedOut => "text-bg-danger",
        AuditEventType.AdminAccessDenied => "text-bg-warning",
        _ => "text-bg-light border"
    };

    public static string Icon(AuditEventType type) => type switch
    {
        AuditEventType.LoginSucceeded => "bi-box-arrow-in-right",
        AuditEventType.LoginFailed => "bi-x-octagon",
        AuditEventType.LockedOut => "bi-lock",
        AuditEventType.Logout => "bi-box-arrow-right",
        AuditEventType.Registered => "bi-person-plus",
        AuditEventType.PasswordChanged or AuditEventType.PasswordResetCompleted => "bi-key",
        AuditEventType.PasswordResetRequested => "bi-envelope",
        AuditEventType.TermsAccepted => "bi-check2-square",
        AuditEventType.AdminAccessDenied => "bi-shield-exclamation",
        _ => "bi-dot"
    };

    public static string Device(DeviceType device) => device switch
    {
        DeviceType.Mobile => "bi-phone",
        DeviceType.Tablet => "bi-tablet",
        DeviceType.Desktop => "bi-laptop",
        _ => "bi-question-circle"
    };
}
