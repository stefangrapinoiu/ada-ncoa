using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Pages.Admin;

/// <summary>Admin overview: users, sign-ins, security alerts and what happened to listings.</summary>
public class IndexModel : PageModel
{
    /// <summary>An IP with this many failed sign-ins in 24h is flagged as suspicious.</summary>
    public const int SuspiciousFailureThreshold = 10;

    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int TotalUsers { get; private set; }
    public int NewUsers7 { get; private set; }
    public int NewUsers30 { get; private set; }
    public int SignedInToday { get; private set; }
    public int SignedIn7 { get; private set; }
    public int SignedIn30 { get; private set; }
    public int FailedLogins24h { get; private set; }
    public int Lockouts24h { get; private set; }
    public IReadOnlyList<(string Ip, int Failures)> SuspiciousIps { get; private set; } = Array.Empty<(string, int)>();

    public int Published { get; private set; }
    public int Available { get; private set; }
    public int Reserved { get; private set; }
    public int HandedOver { get; private set; }
    public int Expired { get; private set; }
    public int Cancelled { get; private set; }
    public int Reservations { get; private set; }
    public int Messages { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since24h = now.AddHours(-24);
        var startOfTodayUtc = RoDate.LocalToUtc(RoDate.Today.ToDateTime(TimeOnly.MinValue));

        TotalUsers = await _db.Users.CountAsync(ct);
        NewUsers7 = await _db.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-7), ct);
        NewUsers30 = await _db.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-30), ct);

        var logins = _db.AuditEvents.Where(a => a.Type == AuditEventType.LoginSucceeded && a.UserId != null);
        SignedInToday = await logins.Where(a => a.OccurredAt >= startOfTodayUtc).Select(a => a.UserId).Distinct().CountAsync(ct);
        SignedIn7 = await logins.Where(a => a.OccurredAt >= now.AddDays(-7)).Select(a => a.UserId).Distinct().CountAsync(ct);
        SignedIn30 = await logins.Where(a => a.OccurredAt >= now.AddDays(-30)).Select(a => a.UserId).Distinct().CountAsync(ct);

        FailedLogins24h = await _db.AuditEvents.CountAsync(a => a.Type == AuditEventType.LoginFailed && a.OccurredAt >= since24h, ct);
        Lockouts24h = await _db.AuditEvents.CountAsync(a => a.Type == AuditEventType.LockedOut && a.OccurredAt >= since24h, ct);

        var suspicious = await _db.AuditEvents
            .Where(a => a.Type == AuditEventType.LoginFailed && a.OccurredAt >= since24h && a.IpAddress != null)
            .GroupBy(a => a.IpAddress!)
            .Select(g => new { Ip = g.Key, Failures = g.Count() })
            .Where(x => x.Failures >= SuspiciousFailureThreshold)
            .OrderByDescending(x => x.Failures)
            .ToListAsync(ct);
        SuspiciousIps = suspicious.Select(x => (x.Ip, x.Failures)).ToList();

        var byStatus = await _db.DonationItems
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        int Count(DonationStatus s) => byStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0;
        Available = Count(DonationStatus.Available);
        Reserved = Count(DonationStatus.Reserved);
        HandedOver = Count(DonationStatus.Completed);
        Expired = Count(DonationStatus.Expired);
        Cancelled = Count(DonationStatus.Cancelled);
        Published = byStatus.Sum(x => x.Count);

        Reservations = await _db.Reservations.CountAsync(ct);
        Messages = await _db.ReservationMessages.CountAsync(ct);
    }

    public static string Percent(int part, int total) => total == 0 ? "—" : $"{Math.Round(100.0 * part / total)}%";
}
