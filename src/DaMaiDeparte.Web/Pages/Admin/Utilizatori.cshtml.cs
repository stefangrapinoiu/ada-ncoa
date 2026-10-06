using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Pages.Admin;

/// <summary>Read-only list of accounts: when they registered, last sign-in, how active they are.</summary>
public class UtilizatoriModel : PageModel
{
    public const int PageSize = 50;

    private readonly ApplicationDbContext _db;

    public UtilizatoriModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    /// <summary>"logare" = last sign-in first (default); "nou" = newest accounts first.</summary>
    [BindProperty(SupportsGet = true, Name = "ordine")]
    public string? Sort { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<Row> Users { get; private set; } = Array.Empty<Row>();

    public int Total { get; private set; }

    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public sealed record Row(string Id, string Name, string? Email, DateTime CreatedAt, DateTime? LastLoginAt, int Listings, int Reservations);

    public async Task OnGetAsync(CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(u => (u.Email != null && u.Email.Contains(s)) || u.FirstName.Contains(s) || u.LastName.Contains(s));
        }

        Total = await query.CountAsync(ct);
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);

        // Anonymous projection so EF can sort by the computed columns in SQL; mapped to Row after.
        var rows = query.Select(u => new
        {
            u.Id,
            Name = u.FirstName + " " + u.LastName,
            u.Email,
            u.CreatedAt,
            LastLoginAt = _db.AuditEvents
                .Where(a => a.UserId == u.Id && a.Type == AuditEventType.LoginSucceeded)
                .Max(a => (DateTime?)a.OccurredAt),
            Listings = _db.DonationItems.Count(d => d.DonatorId == u.Id),
            Reservations = _db.Reservations.Count(r => r.ReceiverId == u.Id)
        });

        rows = Sort == "nou"
            ? rows.OrderByDescending(r => r.CreatedAt)
            : rows.OrderByDescending(r => r.LastLoginAt).ThenByDescending(r => r.CreatedAt);

        var page = await rows
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        Users = page
            .Select(r => new Row(r.Id, r.Name, r.Email, r.CreatedAt, r.LastLoginAt, r.Listings, r.Reservations))
            .ToList();
    }
}
