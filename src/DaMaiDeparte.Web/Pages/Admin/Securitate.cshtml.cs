using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Pages.Admin;

/// <summary>The security log: suspicious IPs (last 24h) and the full, filterable event list.</summary>
public class SecuritateModel : PageModel
{
    public const int PageSize = 50;

    private readonly ApplicationDbContext _db;

    public SecuritateModel(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Event type filter; empty = all.</summary>
    [BindProperty(SupportsGet = true, Name = "tip")]
    public AuditEventType? Type { get; set; }

    /// <summary>Free-text filter: e-mail, IP or user id.</summary>
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<AuditEvent> Events { get; private set; } = Array.Empty<AuditEvent>();

    public int Total { get; private set; }

    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public IReadOnlyList<SuspiciousIpRow> SuspiciousIps { get; private set; } = Array.Empty<SuspiciousIpRow>();

    public sealed record SuspiciousIpRow(string Ip, int Failures, DateTime LastAt, IReadOnlyList<string> Emails);

    public async Task OnGetAsync(CancellationToken ct)
    {
        var since24h = DateTime.UtcNow.AddHours(-24);
        var failures = await _db.AuditEvents
            .AsNoTracking()
            .Where(a => a.Type == AuditEventType.LoginFailed && a.OccurredAt >= since24h && a.IpAddress != null)
            .Select(a => new { a.IpAddress, a.Email, a.OccurredAt })
            .ToListAsync(ct);
        SuspiciousIps = failures
            .GroupBy(f => f.IpAddress!)
            .Where(g => g.Count() >= IndexModel.SuspiciousFailureThreshold)
            .Select(g => new SuspiciousIpRow(
                g.Key,
                g.Count(),
                g.Max(f => f.OccurredAt),
                g.Select(f => f.Email).Where(e => e != null).Select(e => e!).Distinct().Take(10).ToList()))
            .OrderByDescending(r => r.Failures)
            .ToList();

        var query = _db.AuditEvents.AsNoTracking();
        if (Type.HasValue)
        {
            query = query.Where(a => a.Type == Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(a => (a.Email != null && a.Email.Contains(s))
                                     || (a.IpAddress != null && a.IpAddress.Contains(s))
                                     || (a.UserId != null && a.UserId == s));
        }

        Total = await query.CountAsync(ct);
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        Events = await query
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }
}
