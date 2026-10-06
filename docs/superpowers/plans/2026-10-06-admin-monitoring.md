# Admin Monitoring Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a hidden `/Admin` area with a security audit log, usage statistics (overview, impact, geography/devices), a users list and an error log, all stored in the app's own SQL Server.

**Architecture:**
- Three new tables: `AuditEvents`, `UserActivityDays`, `AppLogEntries`.
- Writers:
  - one write path for security events (`IAuditLog`);
  - a once-per-day activity middleware;
  - a channel-backed `ILoggerProvider` for Warning+ logs.
- One read-only `IAdminStatsService` feeds six server-rendered Razor Pages, guarded by an `AdminOnlyFilter` that returns 404.
- A background worker purges old data.

**Tech Stack:** ASP.NET Core 10 Razor Pages, EF Core 10 (SQL Server; SQLite in tests), ASP.NET Core Identity, xUnit, Microsoft.AspNetCore.Mvc.Testing (new, tests only).

**Spec:** `docs/superpowers/specs/2026-10-06-admin-monitoring-design.md`

## Global Constraints
- Web project root: `src/DaMaiDeparte.Web`. Tests: `tests/DaMaiDeparte.Tests`. Namespaces follow folders (`DaMaiDeparte.Web.Monitoring`, `DaMaiDeparte.Web.Pages.Admin`, …).
- **The .NET SDK is not available to Claude.** Every "run tests" step means asking Raluca to run, on her Mac:
  `cd ~/Desktop/DaMaiDeparte/DaMaiDeparte && docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test > ../test-output.txt 2>&1; tail -2 ../test-output.txt`
  Claude then reads `~/mnt/DaMaiDeparte/test-output.txt` via device_bash. To save round-trips, batch "run tests" across consecutive steps of a task.
- **EF migrations** are hand-written, because there is no `dotnet ef`. Each one gets a `.cs` file plus a `.Designer.cs` (a copy of the updated snapshot body with `[Migration("<id>")]`), and `ApplicationDbContextModelSnapshot.cs` is updated. This is the same technique as `20261006120000_AddMessageReadTracking`.
- All UI copy is Romanian. All displayed times go through `RoDate` (Europe/Bucharest).
- Role name: exactly `"Admin"`. The admin area returns **404** (never 403 or a login redirect) to anonymous and non-admin users.
- Retention:
  - `AuditEvents`: 90 days.
  - `AppLogEntries`: 30 days.
  - `UserActivityDays`: 13 months (`DateOnly` older than today minus 13 months).
- Thresholds:
  - Suspicious IP: ≥ 10 `LoginFailed` from the same IP in 24 h.
  - Reliability: ≥ 3 receiver-cancelled or ≥ 2 donor-released reservations in 90 days.
  - Retention windows: days 7–13 and 30–36 after signup, for users who registered 7–60 days ago.
- Column limits:
  - `Email` 256; `IpAddress` 45; `UserAgent` 300; `Details` 500.
  - `Message` 2000; `Exception` 8000; `RequestPath` 300; `Category` 200.
- Logger provider:
  - Captures Warning and above.
  - Excludes categories starting with `Microsoft.EntityFrameworkCore` and its own category.
  - Uses a channel with capacity 1000 in `BoundedChannelFullMode.DropOldest` mode.
- Charts are plain HTML/CSS bars with a `title` tooltip per bar. No JS chart library.
- Service worker `VERSION`: bump `v23` → `v24` once (Task 9).
- Commits end with the attribution lines from the session reminder.

## Review Focus
- **Login with an e-mail that has different casing or surrounding spaces.** The audit row stores the trimmed e-mail as typed, and the success/failure detection must not depend on casing (`PasswordSignInAsync` already normalizes). Test in Task 3.
- **A request through Caddy with several `X-Forwarded-For` hops, or a direct request with none.** The IP is the left-most client address when forwarded; otherwise `RemoteIpAddress`; never Caddy's address. Test in Task 2.
- **The audit table or logger failing (DB down, write exception).** The user's login still succeeds, and the logger never throws into callers. Tests in Tasks 3 and 6.
- **Rows exactly at the retention boundary.** An audit row exactly 90 days old is purged; one 89 days 23 hours old is kept. Test in Task 7.
- **Activity rows when the server's UTC date differs from Bucharest's date** (00:00–03:00 local time). `Day` is the Bucharest date. Test in Task 5.

---

### Task 0: Fix the broken test fixture

**Files:**
- Modify: `tests/DaMaiDeparte.Tests/TestDatabase.cs` and/or the model or config that the error points to

- [ ] **Step 1:** Ask Raluca to run the tests command (Global Constraints). Read `test-output.txt` and find the first `SqliteException` message (e.g. `NOT NULL constraint failed: X` or `FOREIGN KEY constraint failed`).
- [ ] **Step 2:** Find the root cause: which column or constraint the seed users violate, and since which commit (`git log -p` on the referenced model/config). Fix it at the source. Typically that means the fixture's `User(...)` factory sets the newly required field, or the model gets the default it lost. Do not delete or skip tests.
- [ ] **Step 3:** Run the tests. Expected: `Failed: 0` (124 existing tests plus the 6 `NotificationServiceTests` all pass).
- [ ] **Step 4:** Commit: `fix(tests): repair TestDatabase seed users (<root cause>)`.

### Task 1: Monitoring entities, enums and migration

**Files:**
- Create: `src/DaMaiDeparte.Web/Models/AuditEvent.cs`, `Models/UserActivityDay.cs`, `Models/AppLogEntry.cs`, `Models/MonitoringEnums.cs`
- Modify: `Models/Reservation.cs` (add `CancelledBy`), `Data/ApplicationDbContext.cs` (DbSets + configuration)
- Create: `Data/Migrations/20261007090000_AddMonitoring.cs`, `.Designer.cs`. Modify `ApplicationDbContextModelSnapshot.cs`.
- Test: `tests/DaMaiDeparte.Tests/Monitoring/MonitoringSchemaTests.cs`

**Interfaces:**
- Produces:
  - Enums, stored as strings with `HasConversion<string>()` and max length 32:
    - `enum AuditEventType { LoginSucceeded, LoginFailed, LockedOut, Logout, Registered, PasswordChanged, PasswordResetRequested, PasswordResetCompleted, TermsAccepted, AdminAccessDenied }`
    - `enum DeviceType { Unknown, Mobile, Tablet, Desktop }`
    - `enum AppMode { Browser, Installed }`
    - `enum CancelledBy { Receiver, Donor, System }`
  - `class AuditEvent { long Id; DateTime OccurredAt; AuditEventType Type; string? UserId; string? Email; string? IpAddress; string? UserAgent; DeviceType DeviceType; string? Details; }`
  - `class UserActivityDay { long Id; string UserId; DateOnly Day; DeviceType DeviceType; AppMode AppMode; }`, unique index (`UserId`, `Day`, `DeviceType`, `AppMode`), index (`Day`)
  - `class AppLogEntry { long Id; DateTime OccurredAt; string Level; string Category; string Message; string? Exception; string? RequestPath; }`
  - `Reservation.CancelledBy` (`CancelledBy?`)
  - DbSets `AuditEvents`, `UserActivityDays`, `AppLogEntries`
  - Indexes on `AuditEvents`: (`OccurredAt`), (`Type`, `OccurredAt`), (`IpAddress`, `OccurredAt`), (`UserId`)

- [ ] **Step 1: Write the failing test** `Monitoring_tables_round_trip`. Using `TestDatabase`, insert one row of each type plus a reservation with `CancelledBy = Receiver`, then read them back.
  - Assert the values are equal.
  - Assert that inserting a duplicate `UserActivityDay` (same 4-key) throws `DbUpdateException`.
- [ ] **Step 2:** Implement the entities, enums, `OnModelCreating` configuration and max lengths (Global Constraints). Write the hand-written migration `AddMonitoring`, which creates the three tables and the indexes and adds nullable `nvarchar(32)` `CancelledBy` to `Reservations`; then the Designer file and the snapshot update.
- [ ] **Step 3:** Run the tests. Expected: PASS, and existing tests stay green.
- [ ] **Step 4:** Commit `feat(monitoring): add audit, activity and log tables`.

### Task 2: Admin access — forwarded headers, role, 404 filter, grant command, shell

**Files:**
- Create:
  - `Monitoring/AdminRole.cs` (`public const string Name = "Admin";`)
  - `Monitoring/AdminOnlyFilter.cs`
  - `Monitoring/AdminCommands.cs`
  - `Monitoring/RequestInfo.cs`
  - `Pages/Admin/_ViewStart.cshtml`, `Pages/Admin/_AdminLayout.cshtml` (tabs: Prezentare, Impact, Unde și de pe ce, Siguranță, Utilizatori, Erori; it wraps `_Layout`)
  - `Pages/Admin/Index.cshtml(.cs)` (placeholder heading "Prezentare generală")
- Modify:
  - `Program.cs`: forwarded headers before `UseHttpsRedirection`, with `ForwardedHeaders.XForwardedFor | XForwardedProto` and `KnownIPNetworks`/`KnownProxies` cleared. Add the folder filter convention for `/Admin`. Run the admin command before `app.Run()`.
  - `Pages/Shared/_Layout.cshtml`: the "Panou admin" `<li>` in the ☰ menu, shown only when `User.IsInRole(AdminRole.Name)`.
- Modify: `tests/DaMaiDeparte.Tests/DaMaiDeparte.Tests.csproj` (add `Microsoft.AspNetCore.Mvc.Testing` 10.0.0)
- Create: `tests/DaMaiDeparte.Tests/TestAppFactory.cs`, `tests/.../Monitoring/AdminAccessTests.cs`, `tests/.../Monitoring/AdminCommandsTests.cs`, `tests/.../Monitoring/RequestInfoTests.cs`

**Interfaces:**
- Produces:
  - `static class RequestInfo`:
    - `static string? ClientIp(HttpContext ctx)`: returns `ctx.Connection.RemoteIpAddress` after the forwarded-headers middleware, as a string, with IPv4-mapped IPv6 converted to IPv4.
    - `static DeviceType Device(string? userAgent)`: iPad, or Android without "Mobile" → Tablet; iPhone, Android+Mobile or "Mobile" → Mobile; Windows/Macintosh/X11/CrOS → Desktop; else Unknown.
    - `static AppMode Mode(HttpContext ctx)`: cookie `dmd_mode == "installed"` → Installed, else Browser.
  - `class AdminOnlyFilter : IAsyncPageFilter`:
    - not authenticated, or not in role → sets `context.Result = new NotFoundResult()`;
    - when authenticated but not admin, it also writes `AuditEventType.AdminAccessDenied` through `IAuditLog` (resolved from `RequestServices`; ignored if Task 3 isn't in yet, see note).
  - `static class AdminCommands`:
    - `static async Task<int?> TryRunAsync(IServiceProvider services, string[] args, TextWriter output)`: returns null when `args` has no `--grant-admin`/`--revoke-admin`, otherwise an exit code (0 ok, 1 unknown e-mail or usage error).
    - It creates the role if missing and uses `UserManager.FindByEmailAsync`.
  - `class TestAppFactory : WebApplicationFactory<Program>`:
    - environment `"Testing"`;
    - config `Database:ApplyMigrationsOnStartup=false`, `Database:SeedSampleData=false`;
    - replaces `DbContextOptions<ApplicationDbContext>` with SQLite on one open in-memory connection and calls `EnsureCreated`;
    - adds an auth scheme `"Test"`, set as the default authenticate/challenge scheme via `PostConfigure<AuthenticationOptions>`, that signs in the user id from header `X-Test-User` with roles from `X-Test-Roles`;
    - `Task<string> CreateUserAsync(string email, bool admin)` creates the user with `TermsAcceptedAt` set (so `RequireTermsAcceptedFilter` passes) and returns the id.
- Note: until Task 3 adds `IAuditLog`, the filter only returns 404. Task 3 adds the audit write and its test.

- [ ] **Step 1: Write the failing tests:**
  - `AdminAccessTests.Anonymous_gets_404`: GET `/Admin` with no headers → 404.
  - `AdminAccessTests.Signed_in_non_admin_gets_404`: `X-Test-User` set → 404.
  - `AdminAccessTests.Admin_gets_the_panel`: user + `X-Test-Roles: Admin` → 200, body contains "Prezentare generală".
  - `AdminCommandsTests.Grant_admin_adds_role_to_existing_user`: exit 0, `IsInRoleAsync` true.
  - `AdminCommandsTests.Grant_admin_refuses_unknown_email`: exit 1, no user created.
  - `AdminCommandsTests.Revoke_admin_removes_role`.
  - `AdminCommandsTests.No_command_returns_null`: `TryRunAsync(services, ["--urls","x"], …)` is null.
  - `RequestInfoTests.Device_classification`, as a theory:

    | User agent | Expected |
    |---|---|
    | iPhone Safari | Mobile |
    | iPad | Tablet |
    | Android Mobile | Mobile |
    | Android tablet (no "Mobile") | Tablet |
    | Mac Chrome | Desktop |
    | null | Unknown |

- [ ] **Step 2:** Implement everything listed above. `Program.cs`: `if (await AdminCommands.TryRunAsync(app.Services, args, Console.Out) is int code) { Environment.ExitCode = code; return; }`, placed after `DbSeeder.InitializeAsync` and before `app.Run()`.
- [ ] **Step 3:** Run the tests. Expected: PASS.
- [ ] **Step 4:** Commit `feat(admin): hidden admin area, role commands, forwarded headers`.

### Task 3: Security audit log and the Siguranță page

**Files:**
- Create: `Monitoring/IAuditLog.cs`, `Monitoring/AuditLog.cs`, `Pages/Admin/Securitate.cshtml(.cs)`
- Modify:
  - Identity pages: `Areas/Identity/Pages/Account/Login.cshtml.cs`, `Logout.cshtml.cs`, `Register.cshtml.cs`, `ForgotPassword.cshtml.cs`, `ResetPassword.cshtml.cs`, `Manage/ChangePassword.cshtml.cs`, `Pages/Legal/AcceptareTermeni.cshtml.cs`
  - `Monitoring/AdminOnlyFilter.cs`
  - `Program.cs` (DI: `AddHttpContextAccessor`, `AddScoped<IAuditLog, AuditLog>`)
- Create: `Services/IAdminStatsService.cs`, `Services/AdminStatsService.cs`, `Services/AdminDtos.cs` (security part only; Tasks 5 and 8 extend them)
- Test: `tests/.../Monitoring/AuditLogTests.cs`, `tests/.../Monitoring/AdminStatsSecurityTests.cs`

**Interfaces:**
- Produces:
  - `interface IAuditLog { Task WriteAsync(AuditEventType type, string? userId, string? email, string? details = null, CancellationToken cancellationToken = default); }`
    - It fills `OccurredAt = DateTime.UtcNow`, IP/UA/Device from `IHttpContextAccessor` via `RequestInfo`, trims and truncates to the column limits, and lower-cases nothing.
    - It catches every exception and logs a Warning: `"Audit write failed for {Type}"`.
  - Login mapping:
    - `result.Succeeded` → `LoginSucceeded` (userId from `FindByEmailAsync`);
    - `IsLockedOut` → `LockedOut`;
    - otherwise `LoginFailed` with Details `"parolă greșită"` if a user exists for the e-mail, else `"e-mail necunoscut"`.
  - DTOs in `Services/AdminDtos.cs`:
    - `record AuditQuery(AuditEventType? Type, string? Search, string? Ip, DateTime? FromUtc, DateTime? ToUtc, int Page = 1, int PageSize = 50)`
    - `record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)`
    - `record SuspiciousIp(string IpAddress, int Failures, IReadOnlyList<string> Emails, DateTime LastAt)`
    - `record ReliabilityRow(string UserId, string FullName, string? Email, int ReceiverCancelled, int DonorReleased)`
  - `IAdminStatsService` methods:
    - `Task<PagedResult<AuditEvent>> GetAuditAsync(AuditQuery q, CancellationToken ct)`: Search matches Email or UserId by `Contains`; newest first.
    - `Task<IReadOnlyList<SuspiciousIp>> GetSuspiciousIpsAsync(DateTime utcNow, CancellationToken ct)`: ≥ 10 `LoginFailed` in (utcNow − 24h, utcNow].
    - `Task<IReadOnlyList<ReliabilityRow>> GetReliabilityAsync(DateTime utcNow, CancellationToken ct)`: 90 days by `CancelledAt`.
- The Siguranță page has filters (type select, search, IP, from/to dates in dd/MM/yyyy via `RoDate.ParseDate`), the two summary tables and the paged log. Tag colours: ok for success/registered, bad for failed/locked, warn for admin denied, neutral otherwise. Each tag has an icon and a label.
- Reliability data depends on `CancelledBy`, which Task 4 starts writing. Its tests seed the column directly.

- [ ] **Step 1: Write the failing tests:**
  - `AuditLogTests`, via `TestAppFactory` posting the real login form (antiforgery token parsed from the GET):
    - `Successful_login_writes_LoginSucceeded_with_user_and_forwarded_ip`: header `X-Forwarded-For: 203.0.113.7, 10.0.0.2` → one row, `IpAddress == "203.0.113.7"`, `UserId` set.
    - `Wrong_password_writes_LoginFailed_with_detail`: Details `"parolă greșită"`.
    - `Unknown_email_writes_LoginFailed_without_user`: `UserId` null, Email as typed and trimmed, Details `"e-mail necunoscut"`.
    - `Email_with_spaces_and_uppercase_still_logs_in_and_audits`: `"  IOANA@Example.local "` → `LoginSucceeded`.
    - `Fifth_failure_writes_LockedOut`: the 5th wrong attempt's row is `LockedOut`.
    - `Audit_failure_does_not_break_login`: replace `IAuditLog` with one that throws inside `AuditLog`'s underlying save (substitute a DbContext whose `SaveChangesAsync` throws, via `TestAppFactory.WithWebHostBuilder`) → response is a redirect to `/Dashboard`.
    - `Non_admin_hitting_admin_writes_AdminAccessDenied`.
  - `AdminStatsSecurityTests`:
    - `Ten_failures_from_one_ip_in_24h_is_suspicious_nine_is_not`;
    - `Failures_older_than_24h_do_not_count`;
    - `Reliability_flags_three_receiver_cancels_or_two_donor_releases_in_90_days`;
    - `Audit_query_filters_by_type_search_ip_and_dates_and_pages_by_50`.
- [ ] **Step 2:** Implement `IAuditLog`/`AuditLog`. Wire every Identity page in the mapping table of spec §2.1, calling it after the outcome is known. Add the audit write in `AdminOnlyFilter`. Write the security part of `AdminStatsService`.
- [ ] **Step 3:** Implement the `Securitate` page and the alert data source.
- [ ] **Step 4:** Run the tests. Expected: PASS.
- [ ] **Step 5:** Commit `feat(admin): security audit log and Siguranță page`.

### Task 4: Record who cancelled a reservation

**Files:** Modify `Services/ReservationService.cs` (`CancelAsync` line ~108 → `Receiver`; `ReleaseAsync` line ~155 → `Donor`) and `Services/DonationService.cs` (donation cancel ~331 and expiry ~462: any active reservation that gets cancelled → `System`). Test: `tests/.../ReservationServiceTests.cs` (append).

- [ ] **Step 1: Write the failing tests:** `Receiver_cancel_records_CancelledBy_Receiver`, `Donor_release_records_CancelledBy_Donor`, `Cancelling_a_reserved_donation_records_CancelledBy_System`.
- [ ] **Step 2:** Implement: set `reservation.CancelledBy` next to every `CancelledAt = now`. If donation cancel or expiry does not currently cancel the active reservation, only set it where a reservation is actually cancelled. Add no new behaviour.
- [ ] **Step 3:** Run the tests. Expected: PASS.
- [ ] **Step 4:** Commit `feat(reservations): record who cancelled a reservation`.

### Task 5: Daily activity tracking and the Utilizatori page

**Files:**
- Create: `Monitoring/IUserActivityTracker.cs`, `Monitoring/UserActivityTracker.cs`, `Monitoring/UserActivityMiddleware.cs`, `Pages/Admin/Utilizatori.cshtml(.cs)`
- Modify:
  - `Program.cs`: `AddMemoryCache`, `AddScoped<IUserActivityTracker, UserActivityTracker>`, `app.UseMiddleware<UserActivityMiddleware>()` after `UseAuthorization`.
  - `wwwroot/js/site.js`: set `document.cookie = 'dmd_mode=' + (standalone ? 'installed' : 'browser') + '; path=/; max-age=31536000; SameSite=Lax' + (location.protocol === 'https:' ? '; Secure' : '')` on load, reusing the existing standalone detection.
  - `Services/AdminDtos.cs`, `Services/IAdminStatsService.cs`, `Services/AdminStatsService.cs`.
- Test: `tests/.../Monitoring/UserActivityTests.cs`, `tests/.../Monitoring/AdminStatsUsersTests.cs`

**Interfaces:**
- Produces:
  - `interface IUserActivityTracker { Task RecordAsync(string userId, DateTime utcNow, DeviceType device, AppMode mode, CancellationToken ct = default); }`
    - Day is `RoDate.TodayAt(utcNow)`.
    - It skips the database when the `IMemoryCache` key `act:{userId}:{day}:{device}:{mode}` is present (cached until the end of the local day).
    - It inserts the row and treats a unique-violation `DbUpdateException` as success.
    - It never throws (warns instead).
  - The middleware calls it only for authenticated users, skips paths starting with `/css`, `/js`, `/images`, `/lib`, `/uploads`, `/service-worker.js` and `/manifest.webmanifest`, and never awaits failures into the response.
  - `record UserListRow(string Id, string FullName, string? Email, DateTime CreatedAt, DateTime? LastLoginAt, DateOnly? LastActiveDay, int Listings, int Reservations)`
  - `Task<PagedResult<UserListRow>> GetUsersAsync(string? search, string sort, int page, CancellationToken ct)`:
    - sort `"activ"` (`LastActiveDay` desc, the default), `"nou"` (`CreatedAt` desc) or `"nume"`;
    - `LastLoginAt` = max `LoginSucceeded.OccurredAt`;
    - 50 per page.

- [ ] **Step 1: Write the failing tests:**
  - `Many_requests_same_day_create_one_row`: 5 × `RecordAsync`, same inputs → 1 row.
  - `Different_device_or_mode_same_day_creates_separate_rows`.
  - `Day_is_the_Bucharest_date_not_UTC`: `utcNow = 2026-10-06T22:30Z` (01:30 local on 7 Oct) → `Day == 2026-10-07`.
  - `Duplicate_insert_race_is_treated_as_success`: a fresh tracker instance with an empty cache, same key, row already present → no throw, still 1 row.
  - `Users_list_shows_last_login_last_active_and_counts_and_searches_by_email`.
- [ ] **Step 2:** Implement the tracker, the middleware, the cookie line, `GetUsersAsync` and the Utilizatori page (search box, sort select, table, pager).
- [ ] **Step 3:** Run the tests. Expected: PASS.
- [ ] **Step 4:** Commit `feat(admin): daily activity tracking and users page`.

### Task 6: Error log (logger provider) and the Erori page

**Files:**
- Create: `Monitoring/DatabaseLoggerProvider.cs` (provider + logger), `Monitoring/AppLogWriter.cs` (`BackgroundService`), `Pages/Admin/Erori.cshtml(.cs)`
- Modify: `Program.cs` (`builder.Logging.AddProvider(...)` sharing a singleton `Channel<AppLogEntry>`, plus `AddHostedService<AppLogWriter>`), `Services/IAdminStatsService.cs`/`AdminStatsService.cs`
- Test: `tests/.../Monitoring/DatabaseLoggerTests.cs`

**Interfaces:**
- Produces:
  - `sealed class DatabaseLoggerProvider : ILoggerProvider`, ctor `(ChannelWriter<AppLogEntry> writer, IHttpContextAccessor? http)`:
    - `IsEnabled` is true only for `LogLevel.Warning`+ and categories not excluded (Global Constraints);
    - `Log` builds an `AppLogEntry` (truncated) and calls `TryWrite`;
    - it never throws.
  - `sealed class AppLogWriter : BackgroundService`: reads up to 100 entries per batch and saves them in a new scope. On a save failure it drops the batch and writes to `Console.Error` only, never to `ILogger`, to avoid recursion.
  - `Task<PagedResult<AppLogEntry>> GetLogsAsync(string? level, int page, CancellationToken ct)`: newest first, 50 per page.

- [ ] **Step 1: Write the failing tests:**
  - `Warning_and_error_are_captured_information_is_not`.
  - `EntityFramework_and_own_category_are_excluded`.
  - `Full_channel_drops_oldest_and_never_throws`: capacity 3, write 5 → the reader gets the last 3.
  - `Writer_persists_batches`: `AppLogWriter` run against `TestDatabase` for 2 entries → 2 rows.
- [ ] **Step 2:** Implement the provider, the writer, `GetLogsAsync` and the Erori page (level filter, `<details>` rows with the message's first line in the summary and full text in `<pre>`).
- [ ] **Step 3:** Run the tests. Expected: PASS.
- [ ] **Step 4:** Commit `feat(admin): error log captured to the database and Erori page`.

### Task 7: Retention worker

**Files:** Create `Monitoring/IMonitoringRetention.cs`, `Monitoring/MonitoringRetention.cs`, `Monitoring/MonitoringMaintenanceWorker.cs` (same shape as `DonationExpirationWorker`, `Interval = TimeSpan.FromHours(24)`, first run at startup). Modify `Program.cs` (DI + hosted service). Test: `tests/.../Monitoring/RetentionTests.cs`.

**Interfaces:**
- Produces `interface IMonitoringRetention { Task<int> PurgeAsync(DateTime utcNow, CancellationToken ct = default); }`. It returns the total rows deleted and uses `ExecuteDeleteAsync`:
  - `AuditEvents` where `OccurredAt <= utcNow − 90d`;
  - `AppLogEntries` where `OccurredAt <= utcNow − 30d`;
  - `UserActivityDays` where `Day < RoDate.TodayAt(utcNow).AddMonths(-13)`.

- [ ] **Step 1: Write the failing tests:**
  - `Audit_rows_exactly_90_days_old_are_purged_younger_are_kept`: rows at `now−90d` and `now−90d+1h`.
  - `Log_rows_older_than_30_days_are_purged`.
  - `Activity_older_than_13_months_is_purged`.
  - `Purge_returns_number_of_deleted_rows`.
- [ ] **Step 2:** Implement.
- [ ] **Step 3:** Run the tests. Expected: PASS.
- [ ] **Step 4:** Commit `feat(monitoring): daily retention purge`.

### Task 8: Statistics — Prezentare generală, Impact, Geografie

**Files:**
- Create: `Pages/Admin/Impact.cshtml(.cs)`, `Pages/Admin/Geografie.cshtml(.cs)`, `Pages/Admin/_Bars.cshtml` (partial, model `BarSeries`)
- Modify: `Pages/Admin/Index.cshtml(.cs)`, `Services/AdminDtos.cs`, `IAdminStatsService.cs`, `AdminStatsService.cs`, `wwwroot/css/site.css` (admin styles: tiles, bars, tags, tables. Lift the visual language from the approved mockup `panou-admin-macheta.html`: same class names, scoped under `.admin`)
- Test: `tests/.../Monitoring/AdminStatsOverviewTests.cs`, `AdminStatsImpactTests.cs`, `AdminStatsGeoTests.cs`

**Interfaces:**
- Produces:
  - `enum StatsPeriod { Days7, Days30, Days90, All }`, query param `perioada` = `7|30|90|tot` (default 30).
  - `record BarPoint(string Label, int Value)`
  - `record BarSeries(string Title, string Unit, IReadOnlyList<BarPoint> Points)`
  - `record OverviewStats(...)` with these fields:
    - `int TotalUsers, NewUsers, ActiveToday, Active7, Active30, ReservationsInPeriod, MessagesInPeriod, ItemsHandedOverAllTime`
    - `BarSeries ActivePerDay` (30 points, labels `dd MMM`)
    - `BarSeries NewUsersPerWeek` (12 points, labels `S{ISO week}`)
    - `BarSeries ActivityByHour` (24 points, `00`–`23`, Bucharest hour). Each hour counts `LoginSucceeded` audits, donations created, reservations created and messages sent in the period.
    - `double? Retention7, Retention30`: null when there is no eligible cohort.
    - `IReadOnlyList<SuspiciousIp> Suspicious`, `int LockoutsLast24h`
  - `record FunnelStats(int Published, int Reserved, int HandedOver, int Expired, int Cancelled, TimeSpan? MedianToReservation, TimeSpan? MedianToHandover, IReadOnlyList<CategoryFunnelRow> ByCategory)`
    - Cohort = donations with `CreatedAt` in the period.
    - Reserved = donations with at least one reservation.
    - Median to reservation = first `ReservedAt` − `CreatedAt`.
    - Median to handover = `CompletedAt` − `ReservedAt` of the non-cancelled reservation.
    - Medians are computed in memory.
  - `record CategoryFunnelRow(string Category, int Published, int HandedOver, int Expired, TimeSpan? MedianToReservation)`
  - `record GeoStats(IReadOnlyList<BarPoint> ListingsByCity, IReadOnlyList<BarPoint> HandoversByCity, IReadOnlyList<BarPoint> TopNeighborhoods, IReadOnlyList<BarPoint> UsersByCity, IReadOnlyList<BarPoint> ByDevice, IReadOnlyList<BarPoint> ByAppMode)`
    - `ByDevice`/`ByAppMode` count distinct users active in the period.
    - Neighborhood labels are `"{Neighborhood} ({City})"`, top 20.
  - Service methods:
    - `Task<OverviewStats> GetOverviewAsync(StatsPeriod p, DateTime utcNow, CancellationToken ct)`
    - `Task<FunnelStats> GetFunnelAsync(StatsPeriod p, DateTime utcNow, CancellationToken ct)`
    - `Task<GeoStats> GetGeoAsync(StatsPeriod p, DateTime utcNow, CancellationToken ct)`
- Overview UI:
  - The alert banner shows when `Suspicious.Count > 0 || LockoutsLast24h > 0`. It uses the status colour plus an icon plus text and links to `/Admin/Securitate?tip=LoginFailed`.
  - The period selector is links.
  - The headline tile reads "Alimente salvate" (`ItemsHandedOverAllTime` = donations with `Status == Completed`).

- [ ] **Step 1: Write the failing tests** (seeded data, explicit timestamps, `utcNow` fixed at `2026-10-06T12:00Z`):
  - `Overview_counts_total_new_and_active_users_for_period`
  - `Active_per_day_has_30_points_and_counts_distinct_users`
  - `Activity_by_hour_uses_Bucharest_hour`: a message at 21:30Z is counted in hour 00.
  - `Retention_7_counts_users_active_on_days_7_to_13_after_signup`: 2 eligible, 1 returned → 0.5; null when no eligible users.
  - `Funnel_counts_published_reserved_handed_over_expired_cancelled`
  - `Funnel_medians_are_computed_from_timestamps`
  - `Funnel_by_category_matches_totals`
  - `Geo_groups_listings_by_city_and_top_neighborhoods`
  - `Geo_device_and_mode_count_distinct_active_users`
- [ ] **Step 2:** Implement the service methods.
- [ ] **Step 3:** Implement the pages and the `_Bars` partial (bar height = value / max, `title="{Label}: {Value} {Unit}"`, labelled axis ends), plus the CSS.
- [ ] **Step 4:** Run the tests. Expected: PASS.
- [ ] **Step 5:** Commit `feat(admin): overview, impact and geography statistics`.

### Task 9: GDPR text, service worker bump, local verification, merge

**Files:** Modify `Pages/Shared/_LegalContent.cshtml` (one paragraph in the GDPR section: „Pentru protecția conturilor, păstrăm timp de 90 de zile un jurnal de securitate al autentificărilor (data, adresa IP și tipul dispozitivului). Statisticile de utilizare sunt anonime și se păstrează 13 luni.”) and `wwwroot/service-worker.js` (`v23` → `v24`).

- [ ] **Step 1:** Make both edits. Run the tests. Expected: `Failed: 0`.
- [ ] **Step 2:** Raluca runs `docker compose up --build -d` on the Mac. On the local container, run `docker compose exec web dotnet DaMaiDeparte.Web.dll --grant-admin ioana@example.local`; expect "Rolul Admin a fost acordat".
- [ ] **Step 3:** In the Claude browser on http://localhost:8080:
  - as `andrei` (non-admin), `/Admin` shows 404 and no "Panou admin" link;
  - as `ioana`, all six pages render with no errors;
  - one wrong password as andrei appears in Siguranță;
  - the bars render.
- [ ] **Step 4:** Commit. Merge to `dev` (`git checkout dev && git merge --no-ff feature/admin-monitoring`). Raluca pushes, then deploys on the droplet (`deploy.md` recipe) and runs `--grant-admin` there for each of the three admin e-mails that have accounts.
- [ ] **Step 5:** Verify live:
  - `/Admin` is 404 when signed out;
  - Raluca opens "Panou admin" on her phone;
  - update `deploy.md` in the project.
