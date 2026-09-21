# Dă Mai Departe

*Mai puțină risipă alimentară. Mai mult ajutor.*

An MVP Progressive Web App that reduces **household food waste** by letting people offer packaged food they will not consume to someone nearby.

There is **one kind of account**. The same user can publish food today and reserve food tomorrow — no Donator/Beneficiary choice is ever asked for, and no roles are used anywhere.

"Nearby" in Version 1 means **selected city + optional neighborhood**. There is no GPS, no map and no distance calculation.

> The user interface is **Romanian only** (`ro-RO`). Code, identifiers and technical docs are in English.

Primary flow: **Creare cont → Confirmare → Autentificare → Vezi alimentele locale → Adaugă sau rezervă**

Registration does not sign the user in: it ends on a confirmation page and the user logs in with
the credentials they just chose. From then on, login goes **straight to the dashboard** — the
location saved on the account is reused, and is changed from the account settings.

---

## ⚠️ Build status

This solution was written in an environment where the .NET SDK and NuGet were not reachable, so
**it has not been compiled or run yet**. Before anything else, run `dotnet build` and
`dotnet test` locally and fix any compiler errors that come up.

The migration `20260919120000_FoodAndLocationModel` was hand-written for the same reason, together
with its `.Designer.cs` and the model snapshot. After the first successful build, verify it with:

```bash
dotnet ef migrations script --project src/DaMaiDeparte.Web   # inspect the SQL
dotnet ef migrations add Verify --project src/DaMaiDeparte.Web --output-dir Data/Migrations
```

The second command should produce an **empty** migration. If it does not, the snapshot drifted
from the model: delete the generated `Verify` files, and regenerate the snapshot instead.

## ⚠️ Migration data loss

`FoodAndLocationModel` **deletes every existing `DonationItems` and `Reservations` row.**

They describe furniture, clothing and electronics, and the new required columns
(`FoodCategoryId`, `ExpirationDate`, `CityId`, `CountryId`) cannot be derived from a free-text
`PickupArea` or from a non-food category. User accounts, passwords and Identity data are
preserved. Back up the database before running `dotnet ef database update` against anything you
care about.

---

## Requirements

| Component | Version |
|---|---|
| .NET SDK | **10.0** (LTS), see `global.json` |
| SQL Server | 2019+ (Docker image `mcr.microsoft.com/mssql/server:2022-latest`, LocalDB on Windows, or Azure SQL) |
| EF Core CLI | `dotnet-ef` 10.x (`dotnet tool install --global dotnet-ef`) |
| Docker | optional, for the bundled `docker-compose.yml` |

Front-end libraries (Bootstrap 5.3, Bootstrap Icons, jQuery Validation with Romanian messages) are loaded from the jsDelivr CDN. The service worker caches them after the first visit.

## Project structure

```
DaMaiDeparte.sln
src/DaMaiDeparte.Web/           ASP.NET Core Razor Pages app
  Areas/Identity/Pages/Account  Romanian login, registration, logout, account pages (no default Identity UI)
  Data/                         ApplicationDbContext, DbSeeder, Migrations/
  Infrastructure/               constants, food rules, prohibited-food screen, Romanian date
                                formatting, browsing location, Identity + model-binding messages
  Models/                       ApplicationUser, Country/City/Neighborhood, FoodCategory,
                                DonationItem, DonationImage, Reservation, enums
  Pages/                        Index (landing), Dashboard (feed), Location, Donations/*,
                                Reservations/*, Shared
  Resources/UiText.cs           centralized Romanian UI strings
  Services/                     DonationService, ReservationService, LocationService,
                                LocationContext, DonationExpirationWorker, LocalFileStorageService
  wwwroot/                      css, js, icons, manifest.webmanifest, service-worker.js, offline.html, uploads/
tests/DaMaiDeparte.Tests/       xUnit tests (SQLite in-memory)
scripts/                        setup-dev.sh / setup-dev.ps1
```

## Setup

### Quick start (macOS / Linux)

```bash
export MSSQL_SA_PASSWORD='Your_strong_Passw0rd'
docker compose up -d                 # starts SQL Server on localhost:1433
./scripts/setup-dev.sh               # restore, user-secrets, build, create + apply migration
dotnet run --project src/DaMaiDeparte.Web --launch-profile https
```

Open https://localhost:7180.

On Windows use `scripts/setup-dev.ps1` (set `$env:MSSQL_SA_PASSWORD`, or `$env:DMD_CONNECTION` with a full connection string, e.g. LocalDB).

### Manual steps

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
| `Seed:DevUserPassword` | set in `appsettings.Development.json` | Password for the sample users |
| `FileStorage:UploadFolder` | `uploads/donations` | Image folder under `wwwroot` |
| `FileStorage:MaxFileSizeBytes` | `5242880` (5 MB) | Max image size |

Reference data is always seeded (idempotent): România, 10 cities, their neighborhoods, and the
food categories. Re-seeding rewrites the `IsAllowed` flags, so a category that becomes prohibited
cannot stay usable in an older database.

## Seed users (Development only)

| E-mail | Home location | Password |
|---|---|---|
| `ioana@example.local` | Cluj-Napoca · Mărăști | value of `Seed:DevUserPassword` (`Parola.Dev123`) |
| `andrei@example.local` | Cluj-Napoca · Gheorgheni | same |

Both accounts are ordinary accounts — either can publish and either can reserve. Seven sample
packaged-food listings in Cluj-Napoca are created for Ioana (Pâine integrală feliată, Paste penne,
Suc natural de mere, …).

## One account, permissions by relationship

There are no roles and no account types. What a signed-in user may do depends only on their
relationship to a given listing:

| | Owner of the listing | Anyone else | Signed out |
|---|---|---|---|
| See the city feed | ✔ | ✔ | redirected to login |
| Create a listing | ✔ | ✔ | ✖ |
| Edit / cancel a listing | own only, while *Disponibil* | ✖ | ✖ |
| Reserve a listing | **✖ — `Nu poți rezerva propria donație.`** | ✔ if *Disponibil* and not expired | ✖ |
| Cancel a reservation | ✖ | own reservation only | ✖ |
| Set pickup details | ✔ | view only | ✖ |
| Confirm handover | ✔ | ✖ | ✖ |

- Authorization is folder-level authentication (`/Dashboard`, `/Location`, `/Donations/*`,
  `/Reservations/*`); every write re-reads ownership and status from the database.
- Lifecycle: `Available → Reserved → Completed`, `Available → Cancelled`, `Available → Expired`,
  `Reserved → Available` (reservation cancelled). Records are never deleted.
- Contact details and pickup details are visible only to the two participants of a reservation.

## Location model

```
Country (RO — România)
   └── City (mandatory)        Cluj-Napoca, București, Timișoara, Iași, Brașov,
        └── Neighborhood       Sibiu, Oradea, Constanța, Craiova, Târgu Mureș
            (optional)
```

Two locations are tracked separately:

| | Stored in | Changed by |
|---|---|---|
| **Saved location** | `ApplicationUser.Preferred*Id` | registration, then *Locația mea* in account settings |
| **Session cache** | session (`BrowsingLocation`) | refreshed whenever the saved location changes |

The location is a **setting**, not a per-visit question. It is chosen once at registration and
changed from `/Location`, reachable from the account page and from the location bar above the feed.
`/Location` is only ever forced on a user whose account has no usable location at all — for
example if the city they picked was later deactivated.

City and neighborhood names are never hard-coded in a Razor page; they come from the database, and
`City.Latitude`/`Longitude` columns already exist so kilometre-based proximity can be added later
without restructuring.

## Food safety rules

All of these are enforced **server-side**, in `DonationService.ValidateAsync`:

1. **Allowlist, not blacklist.** A listing can only use a `FoodCategory` that is
   `IsActive && IsAllowed`. Meat, fish, dairy, home-cooked food and alcohol exist as seeded rows
   with `IsAllowed = false`, so the prohibition is visible in the data rather than merely absent
   from a dropdown.
2. **Keyword screen (secondary).** `ProhibitedFoodScreen` normalizes the title and description
   (lower-case, diacritics stripped, `ș`/`ț` folded) and matches meat and dairy terms on word
   boundaries. Plant-based phrases — *unt de arahide*, *lapte de migdale* — are excluded first so
   legitimate vegan products are not blocked. This is a guard, not a moderation system.
3. **Packaged only.** Three confirmations are required before publishing: original sealed
   packaging, no meat/fish/dairy, expiry date visible on the packaging.
4. **Expiry is mandatory** and stored as a `DateOnly` (SQL `date`), because it is a Romanian local
   calendar date rather than an instant. It is typed as `zz/ll/aaaa`.
5. **Minimum shelf life of 3 full days.** Food expiring today, tomorrow or the day after is
   rejected; already-expired food is rejected with a different message.
6. **Handover details are required** — a public meeting point, plus optional availability notes.
7. **1 to 3 photos**, JPG/PNG/WebP, selected in a single multi-file field, validated by file
   signature and stored under random names.

### Expiry handling

`RoDate.Today` is the Romanian calendar date, never `DateTime.UtcNow.Date` — the two differ for
part of every evening, which would shift every expiry rule by a day.

Expired food is excluded from the feed **at query level** (`ExpirationDate >= today`), so it
disappears the moment it expires. `DonationExpirationWorker` then sweeps hourly to move those rows
to `Expired` and stamp `ExpiredAt`, which keeps the stored status honest for the owner's own list
and for reporting. Expired listings are archived, never deleted. Food that expires while it is
reserved does not return to the feed if the reservation is cancelled.

## Dashboard feed

The feed is one SQL statement — nothing is filtered in memory:

```
CityId == selected city
AND (NeighborhoodId == selected neighborhood, if one is chosen)
AND Status IN (Available, Reserved)
AND ExpirationDate >= today
```

The status tabs are *Toate* (default, Available + Reserved), *Disponibile* and *Rezervate*.
**Completed, Cancelled and Expired listings never appear under any tab.**

## Concurrency

Reserving runs in a database transaction and is protected twice:

1. `DonationItem.Version` is an optimistic concurrency token (regenerated on every update in `ApplicationDbContext.SaveChanges`). Two simultaneous reservations cannot both update the same row version.
2. A filtered unique index `IX_Reservations_DonationItemId_Active` (`WHERE [CancelledAt] IS NULL`) allows only one active reservation per item.

The loser gets *„Ne pare rău, acest aliment tocmai a fost rezervat de altcineva.”*

## Localization

- Default and only culture: `ro-RO` (request localization + default thread culture).
- Dates are stored in UTC and displayed in `Europe/Bucharest` time, day first:
  `15/09/2026` and `15/09/2026, 18:30` (`Infrastructure/RoDate.cs`).
- Date and time **inputs are plain text** (`zz/ll/aaaa`, `hh:mm`), not `<input type="date">`.
  A native date field renders in the *browser's* locale, so anyone on a US system sees
  `mm/dd/yyyy`, and a page cannot override that. `RoDate.ParseDate` / `ParseTime` accept the
  Romanian order (with `/`, `.` or `-`) plus ISO as a fallback; `site.js` inserts the separators
  while typing. Trade-off: no native calendar picker — a JS datepicker could be added on top.
- Shared Romanian strings live in `Resources/UiText.cs`; page copy is in the Razor views. To add a language later, move `UiText` into `SharedResource.{culture}.resx` and use `IStringLocalizer`.
- Identity errors (`RomanianIdentityErrorDescriber`), model-binding errors (`RomanianModelBindingMessages`), DataAnnotations messages and client-side validation (`messages_ro.js`) are all Romanian.
- The HTML encoder is configured to output Romanian diacritics (ă, â, î, ș, ț) directly.

## Progressive Web App

- `wwwroot/manifest.webmanifest` — name *Dă Mai Departe*, `lang: ro-RO`, `display: standalone`, theme `#198754`, regular + maskable icons.
- `wwwroot/service-worker.js` — pre-caches the offline page and static assets, cache-first for CSS/JS/icons/CDN libraries, network-first for pages with fallback to `offline.html` (*Momentan ești offline*). Donation data and uploads are not cached. POST requests are never intercepted.
- To test installation: run over HTTPS, open Chrome DevTools → Application → Manifest / Service Workers, then use *Install*. On a phone, use a tunnel (e.g. `dev tunnels` or `ngrok`) since PWAs require HTTPS.
- To ship a new service worker version, bump `VERSION` in `service-worker.js`.

## Tests

`tests/DaMaiDeparte.Tests` uses xUnit and SQLite in-memory (real transactions and the filtered
unique index). Covered:

- **Donations & food safety:** any user can publish; the same account can publish and reserve;
  prohibited categories rejected; meat and dairy rejected by keyword even inside an allowed
  category, including from the handover notes; plant-based products (*unt de arahide*, *lapte de
  migdale*) not mistaken for dairy; expired food rejected; 0, 1 and 2 days of shelf life rejected
  while exactly 3 is accepted; due listings become `Expired` and leave the feed; handover location
  required and stored, notes optional; 0 photos and 4 photos rejected, 3 stored in order;
  removing the last photo rejected; a neighborhood from another city rejected; a listing with no
  neighborhood accepted; owner-only editing; reserved listings cannot be edited.
- **Locations & feed:** valid pairs resolve, cross-city and cross-country pairs do not;
  neighborhoods are scoped to their city; the preferred location round-trips through the profile
  and saving it does not affect other users; switching city changes the listings; the
  neighborhood filter narrows the city feed; own listings are marked; paging happens in SQL; only
  allowed categories are offered; cancelled and completed food never appears under any filter.
- **Reservations:** another user can reserve and inherits the listing's handover details;
  reserved food stays visible but not reservable;
  **nobody can reserve their own donation**; expired food cannot be reserved; a stale concurrent
  write is rejected; the database allows only one active reservation; cancelling frees the food
  again, unless it expired meanwhile; a user cannot cancel someone else's reservation; details
  are visible only to the two participants; only the owning donor can set pickup details.
- **Completion:** only the owning donor can complete; available food cannot be completed;
  completion archives the listing for both sides and removes it from every feed filter; completed
  food is never reservable again; summary counts.
- **Helpers:** Romanian status labels; day-first date formatting including a date whose day and
  month could be swapped (`03/02/2026` = 3 February); `dd/MM/yyyy` and `HH:mm` parsing with
  `/`, `.`, `-` and ISO accepted and `6:30 PM` / `13/13/2026` rejected; UTC conversion; expiry
  dates treated as Romanian calendar dates rather than UTC days; remaining-shelf-life wording;
  the prohibited-food screen (case, diacritics, plural suffixes, false-positive guards); image
  signature detection.

## Security notes

- ASP.NET Core Identity (hashed passwords, lockout after 5 failed attempts), secure + HttpOnly cookies, HSTS and HTTPS redirection.
- Anti-forgery validation on every Razor Pages POST.
- Ownership is always read from the database, never from form fields; other users' reservations return 404.
- Uploads: extension + content-type allow-list, file-signature check (JPEG/PNG/WebP), 5 MB limit, random file names, extension derived from content, served with `X-Content-Type-Options: nosniff`.
- Local return URLs only.
- No secrets in source control (`appsettings.json` contains a placeholder; the only committed password is the dev-only sample-user password).

## Out of scope for the MVP

GPS and exact geolocation, maps, distance/route calculation, delivery, chat, ratings, push
notifications, barcode scanning, OCR of expiry dates, AI food recognition, automated food-safety
classification, advanced moderation, cloud storage, countries beyond Romania (the schema supports
them; only the data is missing), multiple languages.
Password reset by e-mail is stubbed (*Ai uitat parola?* shows an informative message) until an e-mail sender is added.
