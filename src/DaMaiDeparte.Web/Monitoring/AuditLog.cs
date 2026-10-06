using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Monitoring;

public sealed class AuditLog : IAuditLog
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditLog> _logger;

    public AuditLog(IServiceScopeFactory scopeFactory, IHttpContextAccessor http, ILogger<AuditLog> logger)
    {
        _scopeFactory = scopeFactory;
        _http = http;
        _logger = logger;
    }

    public async Task WriteAsync(AuditEventType type, string? userId, string? email, string? details = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = _http.HttpContext;
            var userAgent = context?.Request.Headers.UserAgent.ToString();

            // Own scope = own DbContext, so this never saves (or is affected by) whatever the
            // calling page has pending in its DbContext.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AuditEvents.Add(new AuditEvent
            {
                OccurredAt = DateTime.UtcNow,
                Type = type,
                UserId = userId,
                Email = Truncate(email?.Trim(), 256),
                IpAddress = context is null ? null : Truncate(RequestInfo.ClientIp(context), 45),
                UserAgent = Truncate(userAgent, 300),
                DeviceType = RequestInfo.Device(userAgent),
                Details = Truncate(details, 500)
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit write failed for {Type}", type);
        }
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= max ? value : value[..max];
}
