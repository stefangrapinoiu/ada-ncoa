using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// ---------- Culture (ro-RO) ----------
var romanian = new CultureInfo(AppInfo.CultureName);
CultureInfo.DefaultThreadCurrentCulture = romanian;
CultureInfo.DefaultThreadCurrentUICulture = romanian;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(romanian);
    options.SupportedCultures = new List<CultureInfo> { romanian };
    options.SupportedUICultures = new List<CultureInfo> { romanian };
    options.RequestCultureProviders.Clear(); // Romanian-only in V1.
});

// Emit Romanian diacritics (ă, â, î, ș, ț) and „quotes” as-is instead of HTML entities.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(
        UnicodeRanges.BasicLatin,
        UnicodeRanges.Latin1Supplement,
        UnicodeRanges.LatinExtendedA,
        UnicodeRanges.LatinExtendedB,
        UnicodeRanges.GeneralPunctuation));

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Cookies are HTTPS-only by default. The local Docker setup serves plain http://localhost:8080,
// so it sets Security__RequireHttpsCookies=false.
var cookieSecurePolicy = builder.Configuration.GetValue("Security:RequireHttpsCookies", true)
    ? CookieSecurePolicy.Always
    : CookieSecurePolicy.SameAsRequest;

// ---------- Identity ----------
// One account type only: no roles are assigned and no role-based policy exists. The role
// tables stay in the schema (Identity ships them) but the application never reads them.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // No e-mail sending in the MVP.
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<RomanianIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.Name = "DaMaiDeparte.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "DaMaiDeparte.Antiforgery";
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

// ---------- Session (active browsing location) ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "DaMaiDeparte.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true; // Required for the app to work; not used for tracking.
    options.IdleTimeout = TimeSpan.FromHours(8);
});

// ---------- Razor Pages ----------
builder.Services
    .AddRazorPages(options =>
    {
        // Everything except the public landing page requires an account. There is a single
        // user type, so authorization is authentication — no roles, no policies.
        options.Conventions.AuthorizePage("/Dashboard");
        options.Conventions.AuthorizePage("/Location");
        options.Conventions.AuthorizeFolder("/Donations");
        options.Conventions.AuthorizeFolder("/Reservations");
        options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
        options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
    })
    .AddMvcOptions(options => RomanianModelBindingMessages.Apply(options.ModelBindingMessageProvider));

// ---------- Uploads ----------
// Three photos per listing, so the multipart limit covers 3 × 5 MB plus form overhead.
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 20 * 1024 * 1024);

// ---------- E-mail ----------
// No SMTP credentials exist anywhere in this repo yet. When Email:Host is set (e.g. later,
// via user secrets or an environment variable), real e-mail is sent over SMTP; until then,
// password-reset e-mails are just logged (see LoggingEmailSender) so the flow still works
// end-to-end for local testing.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"]))
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
}

// ---------- Application services ----------
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBrowsingLocationStore, SessionBrowsingLocationStore>();
builder.Services.AddScoped<ILocationContext, LocationContext>();
builder.Services.AddHostedService<DonationExpirationWorker>();

var app = builder.Build();

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseRequestLocalization();

var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = context =>
    {
        // The service worker must always be revalidated so updates are picked up.
        if (context.File.Name.Equals("service-worker.js", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }

        context.Context.Response.Headers.XContentTypeOptions = "nosniff";
    }
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

await DbSeeder.InitializeAsync(app.Services, app.Configuration);

app.Run();

/// <summary>Exposed for integration tests.</summary>
public partial class Program
{
}
