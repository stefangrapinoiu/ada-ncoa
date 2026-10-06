# Admin monitoring panel — design

Date: 2026-10-06 · Status: approved in conversation, awaiting written-spec review · Owner: Raluca

## Goal
Give Raluca and the team one hidden place to monitor Adă-nCoa. It should answer six questions:
- Who signs in, and when?
- Are there security incidents (failed logins, lockouts, brute-force attempts)?
- How much is the app used, and by whom?
- What impact do listings have (how much food is actually handed over)?
- Where, and on what devices, is the app used?
- Which technical errors happen?

**Audience:** the internal team only.

**Constraints:**
- a single 2 vCPU / 4 GB droplet;
- personal data (e-mail, IP) under GDPR;
- a non-technical owner;
- nothing new to host.

## Decisions (from brainstorming)
- **Hidden admin area inside the app (approach A).** Data stays in the app's own SQL Server.
  - Rejected: self-hosted Seq/Umami, because of RAM on the droplet.
  - Rejected: external Better Stack/PostHog, because personal data would go to third parties and need a cookie banner.
- **Single write path:** all events go through one service (`IAuditLog`). An external sink can be added later without touching call sites.
- **Out of scope:**
  - admin actions (block a user, hide a listing);
  - CSV export;
  - a server health page;
  - push or e-mail alerts.

## 1. Access

**Admin role**
- Uses an Identity role `Admin`. The role tables already exist and are unused today.
- `AdminOnlyFilter`, a page filter added to the `/Admin` folder by convention, guards every `/Admin/**` page:
  - If the user is not authenticated or lacks the role, it returns **404**, not a redirect to login, so the area's existence is not revealed.
  - Each such attempt is logged as `AdminAccessDenied`.
- The "Panou admin" link in the ☰ menu renders only for admins.

**Granting the role**
- Run the app's command-line mode on the server:
  - `docker compose exec web dotnet DaMaiDeparte.Web.dll --grant-admin <email>`
  - `--revoke-admin <email>` removes the role.
- It works only for **existing** accounts. It prints the result and exits without starting the web server.
- There is deliberately no config e-mail list. Registration does not verify e-mail, so anyone could register a listed address before its owner and become admin.
- **Initial admins:** ralucamusty@gmail.com, raluca.balabuc@gmail.com, stefan.severian@proton.me. Each is granted only once their account exists on the server.

**Client IP behind Caddy**
- Today the app sees Caddy's address on every request.
- Fix: add `UseForwardedHeaders` (X-Forwarded-For and X-Forwarded-Proto) and trust any proxy.
- This is safe because port 8080 is bound to `127.0.0.1`, so only Caddy can reach the app.

## 2. Data recorded

### 2.1 `AuditEvents` (security log)

| Column | Notes |
|---|---|
| `Id` | |
| `OccurredAt` | UTC |
| `Type` | string enum, see the table below |
| `UserId` | nullable |
| `Email` | as typed; nullable, max 256 |
| `IpAddress` | max 45 |
| `UserAgent` | max 300 |
| `DeviceType` | Mobile, Tablet, Desktop or Unknown, derived from the user agent |
| `Details` | short free text, max 500 |

**Indexes:** (`OccurredAt`), (`Type`, `OccurredAt`), (`IpAddress`, `OccurredAt`), (`UserId`).

**Types and where they are written:**

| Type | Written from |
|---|---|
| `LoginSucceeded`, `LoginFailed`, `LockedOut` | `Login.cshtml.cs`, based on the result of `PasswordSignInAsync`. For `LoginFailed`, `Details` says whether it was a wrong password or an unknown e-mail. |
| `Logout` | `Logout.cshtml.cs` |
| `Registered` | `Register.cshtml.cs` |
| `PasswordChanged` | `Manage/ChangePassword.cshtml.cs` |
| `PasswordResetRequested`, `PasswordResetCompleted` | `ForgotPassword` and `ResetPassword` |
| `TermsAccepted` | `Legal/AcceptareTermeni` |
| `AdminAccessDenied` | `AdminOnlyFilter` |

**Retention:** 90 days. A daily cleanup deletes older rows. It runs in a new `MonitoringMaintenanceWorker`, which follows the `DonationExpirationWorker` pattern.

**Failure isolation:** an audit write never breaks the user's action. Write failures are caught and logged at Warning level.

### 2.2 `UserActivityDays` (active users, devices, app mode)
- **Columns:** `UserId`, `Day` (local date in Bucharest), `DeviceType`, `AppMode` (Browser or Installed).
- **Unique key:** (`UserId`, `Day`, `DeviceType`, `AppMode`).
- **No IP is stored.**
- **How it's written:** a small middleware, for authenticated requests only.
  - An in-memory cache, keyed by the same four values, means at most one database write per combination per day.
  - The insert is idempotent, so duplicates are ignored.
- **`AppMode`** comes from a first-party cookie, `dmd_mode`. On page load, `site.js` sets it from `display-mode: standalone` or `navigator.standalone`.
- **Retention:** 13 months.

### 2.3 `AppLogEntries` (technical errors)
- **Columns:** `Id`, `OccurredAt`, `Level`, `Category`, `Message` (max 2000), `Exception` (max 8000), `RequestPath` (max 300).
- **What's captured:** a custom `ILoggerProvider` collects **Warning and above** from the app and ASP.NET categories.
- **What's excluded:** `Microsoft.EntityFrameworkCore.*` and the provider's own category, to avoid recursion and noise.
- **How it's written:** entries go into a bounded in-memory channel, and a background writer flushes them in batches.
  - The channel holds 1,000 entries and drops the oldest when full.
  - Logging never blocks or fails a request.
- **Retention:** 30 days.

### 2.4 Domain change for "reliability"
- New column: `Reservation.CancelledBy`, a nullable string with three values:

| Value | Set when |
|---|---|
| `Receiver` | the receiver cancels (`CancelAsync`) |
| `Donor` | the donor releases a no-show (`ReleaseAsync`) |
| `System` | the donation is cancelled or expires |

- Existing cancelled rows stay null and count as "unknown".

**Migration:** a single migration, `AddMonitoring`, adds the three tables and `Reservation.CancelledBy`.

## 3. Admin pages

**Applies to every page:**
- in Romanian, server-rendered, mobile-friendly, under `/Admin`;
- charts are plain HTML/CSS bars with the numbers labelled, no chart library;
- all times in Europe/Bucharest;
- the overview has a period selector: 7, 30 or 90 days, or all time.

**1. Prezentare generală (`/Admin`)**
- **Alert banner** when, in the last 24 h, any IP had 10 or more failed logins, or any account was locked out. It links to the filtered security log.
- **Users:**
  - total, and new in the selected period;
  - active today, in the last 7 days and in the last 30 days (distinct users in `UserActivityDays`);
  - a chart of new users per week, over 12 weeks;
  - a chart of active users per day, over 30 days.
- **Activity by hour of day:** logins, published listings, reservations and messages, per hour from 0 to 23, over the selected period.
- **Retention:** of users who registered between 7 and 60 days ago, the % who came back on days 7–13 after signing up, and on days 30–36 when enough time has passed. This data only exists from the deploy onward, so the numbers fill in over time.

**2. Impact (`/Admin/Impact`)**
- **Funnel for the period:** published, then reserved, then handed over (Completed), with counts and percentages. Also shown: expired without pickup (%) and cancelled.
- **Median times:** from publishing to the first reservation, and from reservation to handover.
- **The same breakdown per food category.**
- **Headline:** total food handed over, all time ("alimente salvate").

**3. Unde și de pe ce (`/Admin/Geografie`)**
- Listings and handovers per city and per neighborhood (top 20).
- Users by preferred city.
- Active users by device type, and by app mode (installed or browser), over the selected period.

**4. Siguranță (`/Admin/Securitate`)**
- **The audit log:** filter by type, e-mail or user, IP, and date range. 50 rows per page, newest first.
- **"IP-uri suspecte":** IPs with 10 or more failed logins in 24 h, with counts and the e-mails tried.
- **"Încredere":** users with, in the last 90 days, either:
  - 3 or more reservations they cancelled themselves, or
  - 2 or more reservations the donor released as a no-show.

**5. Utilizatori (`/Admin/Utilizatori`)**
- **Columns:** name, e-mail, registration date, last login (from `AuditEvents`), last active day, number of listings, number of reservations.
- Search by name or e-mail, sortable, 50 per page.
- Read-only.

**6. Erori (`/Admin/Erori`)**
- Newest first: time, level, category, first line of the message, request path.
- Click a row to see the full message and exception.
- Filter by level.

**Data source:** a new `IAdminStatsService` runs read-only queries over the existing tables plus the new ones. No caching for now, because the data volumes are small.

## 4. Testing

**Automated tests** use SQLite with the existing `TestDatabase` pattern, plus `WebApplicationFactory` where pages are involved.

- **Access**
  - Anonymous user gets 404.
  - Signed-in non-admin gets 404, and an `AdminAccessDenied` event is written.
  - Admin gets 200.
  - `--grant-admin` works for an existing account and refuses an unknown e-mail.
- **Audit**
  - Successful login, wrong password, unknown e-mail and lockout each write exactly one event, with the correct type, user or e-mail, and IP (honoring `X-Forwarded-For`).
  - An audit write failure does not fail the login.
- **Activity:** one row per user, day, device and mode, even when many requests arrive the same day.
- **Stats:** checked on seeded data with explicit timestamps:
  - funnel counts and percentages, medians, the per-category split;
  - active-user counts and the retention calculation;
  - the suspicious-IP and reliability thresholds.
- **Retention cleanup:** deletes audit rows older than 90 days, log entries older than 30 days and activity older than 13 months, and keeps newer rows.
- **Logger provider:** drops entries when the channel is full and never throws into the caller.

**Known blocker:** 75 existing tests currently fail in the `TestDatabase` constructor, with a SQLite error when seeding users. This must be fixed first (phase 0), so the new tests mean something.

**Verification order:**
1. Tests in the SDK container on the Mac.
2. A local `docker compose` run with the demo accounts.
3. Deploy to the server.
4. Grant the admin roles.
5. Raluca checks the panel on her phone.

## 5. Phases
Each phase is one commit on `feature/admin-monitoring`. The branch is merged to `dev` only once all phases are done and verified.

0. Fix the broken test fixture.
1. Access and shell: forwarded headers, the `Admin` role, `AdminOnlyFilter`, `--grant-admin` and `--revoke-admin`, an empty admin area with navigation, and the menu link.
2. Security log: `AuditEvents` and `IAuditLog`, wired into the Identity pages; the Siguranță page.
3. Activity: the `UserActivityDays` middleware and the `dmd_mode` cookie; `CancelledBy`; the Utilizatori page.
4. Errors: the `AppLogEntries` logger provider, the Erori page, and retention in `MonitoringMaintenanceWorker`.
5. Statistics: `IAdminStatsService`, and the Prezentare generală, Impact and Geografie pages.

## Privacy / GDPR
- IPs and e-mails live only in `AuditEvents`, for 90 days. Activity rows have no IP. Only admins can see any of this data.
- The GDPR text (`_LegalContent.cshtml`) gets one new paragraph saying that:
  - security logs (sign-ins, IP address, device type) are kept for 90 days to protect accounts;
  - anonymous usage statistics are kept for 13 months.
- `dmd_mode` is a functional first-party cookie, not used for tracking, so no consent banner is needed.

## Risks

| Risk | Mitigation |
|---|---|
| Extra database writes from activity tracking | The per-day cache limits it to a few inserts per user per day. |
| A broken loop flooding the log | Warning-and-above filter, bounded channel, batching, 30-day retention. |
| Stats queries slowing down as data grows | The indexes above cover it for now; add caching later if needed. |
