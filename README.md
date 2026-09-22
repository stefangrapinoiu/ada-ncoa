# Ada-ncoa

*Ultima oprire înainte de risipă.*

An MVP Progressive Web App that reduces household **food** waste by letting people give away food they will not consume in time.
**Donators** publish food items (with a best-before date and an optional quantity); **Receivers** (UI: *Beneficiari*) browse and reserve them; both arrange the pickup; the donator confirms the handover and the item is archived in both users' history.

> The user interface is **Romanian only** (`ro-RO`). Code, identifiers and technical docs are in English.

Workflow: **Publică → Descoperă → Rezervă → Stabilește predarea → Predă → Finalizează → Arhivează**

---

## ⚠️ Build status

This solution was written in an environment where the .NET SDK and NuGet were not reachable, so **it has not been compiled or run yet**.
The first `docker compose up --build` (or `dotnet build`) is the first real compile — expect to fix a few errors.
No EF Core migration is included yet: the Docker setup creates the schema automatically; for Option B the setup script generates the `InitialCreate` migration.

---

## Requirements

| Component | Version |
|---|---|
| .NET SDK | **10.0** (LTS), see `global.json` — only for Option B / tests |
| SQL Server | 2019+ (Docker image `mcr.microsoft.com/mssql/server:2022-latest`, LocalDB on Windows, or Azure SQL) |
| EF Core CLI | `dotnet-ef` 10.x (`dotnet tool install --global dotnet-ef`) |
| Docker Desktop | required for Option A (runs app + database); used for SQL Server in Option B |

Front-end libraries (Bootstrap 5.3, Bootstrap Icons, jQuery Validation with Romanian messages) are loaded from the jsDelivr CDN. The service worker caches them after the first visit.

## Project structure

```
DaMaiDeparte.sln
src/DaMaiDeparte.Web/           ASP.NET Core Razor Pages app
  Areas/Identity/Pages/Account  Romanian login, registration, logout, account pages (no default Identity UI)
  Data/                         ApplicationDbContext, DbSeeder, Migrations/
  Infrastructure/               constants, Romanian date formatting, Identity + model-binding messages
  Models/                       ApplicationUser, Category, DonationItem, Reservation, enums
  Pages/                        Home, Donations (public), Donator/*, Receiver/*, Shared
  Resources/UiText.cs           centralized Romanian UI strings
  Services/                     DonationService, ReservationService, LocalFileStorageService
  wwwroot/                      css, js, icons, manifest.webmanifest, service-worker.js, offline.html, uploads/
tests/DaMaiDeparte.Tests/       xUnit tests (SQLite in-memory)
scripts/                        setup-dev.sh / setup-dev.ps1
```

## Setup

### Option A — Docker only (recommended)

The only thing you need to install is **Docker Desktop**.
On Apple-chip Macs, enable *Settings → General → Use Rosetta for x86_64/amd64 emulation* (SQL Server needs it).

```bash
cd DaMaiDeparte
docker compose up --build
```

Wait until the log shows `Now listening on: http://[::]:8080`, then open **http://localhost:8080**.
The first run takes several minutes (it downloads the .NET and SQL Server images and builds the app).

What happens:

- `sqlserver` starts SQL Server 2022 and waits until it is healthy;
- `web` builds the app from the `Dockerfile`, creates the database (migrations if present, otherwise `EnsureCreated`) and seeds roles, categories, sample users and sample products;
- product photos are kept in the `damaideparte-uploads` volume, the database in `damaideparte-sql-data`.

Useful commands:

| Command | What it does |
|---|---|
| `docker compose up --build` | build and start everything (use again after code changes) |
| `docker compose up -d` | start in the background |
| `docker compose logs -f web` | follow the app log |
| `docker compose down` | stop (data is kept) |
| `docker compose down -v` | stop and delete all data (fresh database next time) |

The database password is a local development default defined in `docker-compose.yml`. To use your own, create a `.env` file next to it with `MSSQL_SA_PASSWORD=YourOwn_Strong_Passw0rd` (and run `docker compose down -v` once if the database was already created with the old password).

> If you later add EF Core migrations to a database that was created by `EnsureCreated`, reset it with `docker compose down -v`.

### Option B — run the app with the .NET SDK (for development)

Requires the .NET 10 SDK in addition to Docker.

#### Quick start (macOS / Linux)

```bash
export MSSQL_SA_PASSWORD='Your_strong_Passw0rd'
docker compose up -d sqlserver       # starts only SQL Server on localhost:1433
./scripts/setup-dev.sh               # restore, user-secrets, build, create + apply migration
dotnet run --project src/DaMaiDeparte.Web --launch-profile https
```

Open https://localhost:7180.

On Windows use `scripts/setup-dev.ps1` (set `$env:MSSQL_SA_PASSWORD`, or `$env:DMD_CONNECTION` with a full connection string, e.g. LocalDB).

#### Manual steps

1. **Restore**
   ```bash
   dotnet restore
   ```
2. **Connection string** — never commit real credentials. Use user-secrets for development:
   ```bash
   dotnet user-secrets --project src/DaMaiDeparte.Web set "ConnectionStrings:DefaultConnection" \
     "Server=localhost,1433;Database=DaMaiDeparte_Dev;User Id=sa;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
   ```
   In production, set the environment variable `ConnectionStrings__DefaultConnection`.
3. **Build**
   ```bash
   dotnet build
   ```
4. **Migrations** (first time only — creates `Data/Migrations/*_InitialCreate.cs`)
   ```bash
   export ASPNETCORE_ENVIRONMENT=Development   # so user-secrets are used
   dotnet ef migrations add InitialCreate --project src/DaMaiDeparte.Web --output-dir Data/Migrations
   dotnet ef database update --project src/DaMaiDeparte.Web
   ```
   After changing the model: `dotnet ef migrations add <Name> --project src/DaMaiDeparte.Web --output-dir Data/Migrations`.
5. **Run**
   ```bash
   dotnet run --project src/DaMaiDeparte.Web --launch-profile https
   ```
6. **Test**
   ```bash
   dotnet test
   ```

### Configuration

| Key | Default | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | placeholder | SQL Server connection |
| `Database:ApplyMigrationsOnStartup` | `false` (`true` in Development) | Runs `Migrate()` at startup |
| `Database:SeedSampleData` | `false` (`true` in Development) | Creates sample users and donations |
| `Security:RequireHttpsCookies` | `true` (`false` in Docker) | HTTPS-only auth cookies |
| `Seed:DevUserPassword` | set in `appsettings.Development.json` | Password for the sample users |
| `FileStorage:UploadFolder` | `uploads/donations` | Image folder under `wwwroot` |
| `FileStorage:MaxFileSizeBytes` | `5242880` (5 MB) | Max image size |

Roles (`Donator`, `Receiver`) and the 8 Romanian food categories (Lactate, Legume și fructe, Carne și pește, Conserve, Panificație, Băuturi, Dulciuri, Altele) are always seeded (idempotent).

## Seed users (Development only)

| E-mail | Role | Password |
|---|---|---|
| `donator@example.local` | Donator | value of `Seed:DevUserPassword` (`Parola.Dev123`) |
| `receiver@example.local` | Receiver | same |

Seven sample food donations in Romanian are created for the donator (Lapte, Mere roșii din grădină, Conserve de fasole, Pâine de casă, …), with best-before dates relative to today.

## Roles and rules

| | Donator | Receiver (Beneficiar) | Anonymous |
|---|---|---|---|
| Browse / search / details | ✔ | ✔ | ✔ |
| Create, edit (while *Available*), cancel donation | own only | ✖ | ✖ |
| Reserve | ✖ | ✔ | redirected to login |
| Cancel reservation | ✖ | own only | ✖ |
| Set pickup details | own donations | view only | ✖ |
| Confirm handover (*Confirmă predarea*) | own donations | ✖ | ✖ |
| History | *Istoricul donațiilor* | *Produse primite* | ✖ |

- The role is chosen at registration (*Vreau să donez produse* / *Vreau să primesc produse*) and cannot be changed in the MVP.
- Folder-level authorization: `/Donator/*` requires `Donator`, `/Receiver/*` requires `Receiver`. Services re-check the user's account type, ownership and the current status on every write.
- Lifecycle: `Available → Reserved → Completed`, `Available → Cancelled`, `Reserved → Available` (receiver cancels). Records are never deleted.
- Food past its best-before date (`ExpiresOn`) never appears in the public feed, and the feed can be sorted by closest expiry.
- Contact details (phone, e-mail) and pickup details are visible only to the two participants of an active reservation.

## Concurrency

Reserving runs in a database transaction and is protected twice:

1. `DonationItem.Version` is an optimistic concurrency token (regenerated on every update in `ApplicationDbContext.SaveChanges`). Two simultaneous reservations cannot both update the same row version.
2. A filtered unique index `IX_Reservations_DonationItemId_Active` (`WHERE [CancelledAt] IS NULL`) allows only one active reservation per item.

The loser gets *„Ne pare rău, acest produs tocmai a fost rezervat de altcineva.”*

## Localization

- Default and only culture: `ro-RO` (request localization + default thread culture).
- Dates are stored in UTC and displayed in `Europe/Bucharest` time, e.g. `15 septembrie 2026, 18:30` (`Infrastructure/RoDate.cs`).
- Shared Romanian strings live in `Resources/UiText.cs`; page copy is in the Razor views. To add a language later, move `UiText` into `SharedResource.{culture}.resx` and use `IStringLocalizer`.
- Identity errors (`RomanianIdentityErrorDescriber`), model-binding errors (`RomanianModelBindingMessages`), DataAnnotations messages and client-side validation (`messages_ro.js`) are all Romanian.
- The HTML encoder is configured to output Romanian diacritics (ă, â, î, ș, ț) directly.

## Progressive Web App

- `wwwroot/manifest.webmanifest` — name *Ada-ncoa*, `lang: ro-RO`, `display: standalone`, theme `#198754`, regular + maskable icons.
- `wwwroot/service-worker.js` — pre-caches the offline page and static assets, cache-first for CSS/JS/icons/CDN libraries, network-first for pages with fallback to `offline.html` (*Momentan ești offline*). Donation data and uploads are not cached. POST requests are never intercepted.
- To test installation: run over HTTPS, open Chrome DevTools → Application → Manifest / Service Workers, then use *Install*. On a phone, use a tunnel (e.g. `dev tunnels` or `ngrok`) since PWAs require HTTPS.
- To ship a new service worker version, bump `VERSION` in `service-worker.js`.

## Tests

`tests/DaMaiDeparte.Tests` uses xUnit and SQLite in-memory (real transactions and the filtered unique index). Covered:

- **Donations:** donator can create; receiver cannot; unknown category rejected; expired food rejected and hidden from the feed; sorting by closest expiry; donator cannot edit someone else's donation; reserved items cannot be edited; cancellation archives and hides the item; search, filters, sorting and paging; dashboard counts.
- **Reservations:** receiver can reserve; reserved item cannot be reserved again; stale concurrent write is rejected; database allows only one active reservation; donators cannot reserve; cancelled/missing items cannot be reserved; receiver can cancel and the item becomes available again; cannot cancel another receiver's reservation; private details only for participants; only the owner can set pickup details.
- **Completion:** only the owner can complete; available items cannot be completed; completion archives the item, removes it from the feed and shows it in both histories; completed items can never be reserved again.
- **Helpers:** Romanian labels, Romanian date formatting and UTC conversion, image signature detection.

## Security notes

- ASP.NET Core Identity (hashed passwords, lockout after 5 failed attempts), secure + HttpOnly cookies, HSTS and HTTPS redirection.
- Anti-forgery validation on every Razor Pages POST.
- Ownership is always read from the database, never from form fields; other users' reservations return 404.
- Uploads: extension + content-type allow-list, file-signature check (JPEG/PNG/WebP), 5 MB limit, random file names, extension derived from content, served with `X-Content-Type-Options: nosniff`.
- Local return URLs only.
- No secrets in source control (`appsettings.json` contains a placeholder; the only committed password is the dev-only sample-user password).

## Out of scope for the MVP

Chat, SignalR, push/e-mail/SMS notifications, maps/GPS, reviews, payments, delivery, AI, admin analytics, native apps, multiple images, cloud storage, multiple languages.
Password reset by e-mail is stubbed (*Ai uitat parola?* shows an informative message) until an e-mail sender is added.
