using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Data;

public static class DbSeeder
{
    public const string DevUserOneEmail = "ioana@example.local";
    public const string DevUserTwoEmail = "andrei@example.local";

    /// <summary>Version 1 activates Romania only; further countries are added here, not in code paths.</summary>
    public static readonly (string Code, string Name)[] Countries =
    {
        ("RO", "România")
    };

    /// <summary>Slugs are diacritic-free and unique inside the country.</summary>
    public static readonly (string Slug, string Name)[] RomanianCities =
    {
        ("bucuresti", "București"),
        ("brasov", "Brașov"),
        ("cluj-napoca", "Cluj-Napoca"),
        ("constanta", "Constanța"),
        ("craiova", "Craiova"),
        ("iasi", "Iași"),
        ("oradea", "Oradea"),
        ("sibiu", "Sibiu"),
        ("targu-mures", "Târgu Mureș"),
        ("timisoara", "Timișoara")
    };

    /// <summary>
    /// Neighborhoods are optional everywhere. A city with no rows here simply shows no
    /// neighborhood filter — nothing else in the application changes.
    /// </summary>
    public static readonly (string CitySlug, string[] Names)[] Neighborhoods =
    {
        ("cluj-napoca", new[]
        {
            "Borhanci", "Bună Ziua", "Centru", "Gheorgheni", "Grigorescu",
            "Iris", "Mănăștur", "Mărăști", "Zorilor"
        }),
        ("bucuresti", new[]
        {
            "Sector 1", "Sector 2", "Sector 3", "Sector 4", "Sector 5", "Sector 6"
        }),
        ("timisoara", new[] { "Centru", "Complex Studențesc", "Fabric", "Girocului", "Iosefin", "Lipovei" }),
        ("iasi", new[] { "Centru", "Copou", "Nicolina", "Păcurari", "Tătărași" }),
        ("brasov", new[] { "Astra", "Bartolomeu", "Centrul Vechi", "Racadau", "Tractorul" }),
        ("sibiu", new[] { "Centru", "Hipodrom", "Terezian", "Vasile Aaron" }),
        ("oradea", new[] { "Centru", "Iosia", "Nufărul", "Rogerius" }),
        ("constanta", new[] { "Centru", "Faleză Nord", "Mamaia", "Tomis Nord" }),
        ("craiova", new[] { "Brazda lui Novac", "Centru", "Craiovița Nouă", "Rovine" }),
        ("targu-mures", new[] { "Centru", "Dâmbul Pietros", "Tudor Vladimirescu", "Unirii" })
    };

    /// <summary>
    /// The category allowlist. Meat and dairy are seeded as rows with <c>IsAllowed = false</c>
    /// on purpose: the rule is then visible in the data and enforced by the service layer,
    /// rather than being a category that merely does not exist in a dropdown.
    /// </summary>
    public static readonly (string Key, string Name, string? Description, bool IsAllowed)[] FoodCategories =
    {
        ("PackagedBakery", "Panificație ambalată",
            "Pâine, lipii, biscuiți sau produse de panificație ambalate și sigilate.", true),
        ("PackagedProduce", "Fructe și legume ambalate",
            "Fructe și legume în ambalaj original, sigilat, cu termen de valabilitate vizibil.", true),
        ("PackagedPantry", "Produse alimentare vegetale ambalate",
            "Paste, orez, leguminoase, conserve vegetale, ulei, făină, zahăr — toate sigilate.", true),
        ("PackagedSnacks", "Gustări ambalate",
            "Batoane, covrigi, semințe, fructe uscate sau alte gustări sigilate.", true),
        ("PackagedBeverages", "Băuturi nealcoolice ambalate",
            "Apă, sucuri, ceai sau cafea în ambalaj original, sigilat. Fără alcool.", true),
        ("PackagedBabyFood", "Alimente ambalate pentru bebeluși",
            "Piureuri și cereale pentru bebeluși, sigilate, cu termen de valabilitate lung.", true),
        ("OtherApproved", "Alte alimente ambalate aprobate",
            "Doar dacă alimentul nu se încadrează în categoriile de mai sus. Denumește clar produsul.", true),

        // Present but never selectable — see IsAllowed = false.
        ("Meat", "Carne și produse din carne", "Categorie interzisă pe platformă.", false),
        ("Fish", "Pește și fructe de mare", "Categorie interzisă pe platformă.", false),
        ("Dairy", "Lactate", "Categorie interzisă pe platformă.", false),
        ("HomeCooked", "Mâncare gătită în casă", "Categorie interzisă în această versiune.", false),
        ("Alcohol", "Băuturi alcoolice", "Categorie interzisă pe platformă.", false)
    };

    /// <summary>Applies migrations (optional) and seeds reference data and — optionally — development data.</summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));
        var db = provider.GetRequiredService<ApplicationDbContext>();

        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync();
        }

        await SeedLocationsAsync(db);
        await SeedFoodCategoriesAsync(db);

        if (configuration.GetValue<bool>("Database:SeedSampleData"))
        {
            var password = configuration["Seed:DevUserPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Seed:DevUserPassword is not configured; sample users were not created");
                return;
            }

            await SeedSampleDataAsync(db, provider.GetRequiredService<UserManager<ApplicationUser>>(), password, logger);
        }
    }

    public static async Task SeedLocationsAsync(ApplicationDbContext db)
    {
        foreach (var (code, name) in Countries)
        {
            if (!await db.Countries.AnyAsync(c => c.Code == code))
            {
                db.Countries.Add(new Country { Code = code, Name = name, IsActive = true });
            }
        }

        await db.SaveChangesAsync();

        var romania = await db.Countries.FirstAsync(c => c.Code == AppInfo.DefaultCountryCode);

        var existingCities = await db.Cities
            .Where(c => c.CountryId == romania.Id)
            .Select(c => c.Slug)
            .ToListAsync();

        foreach (var (slug, name) in RomanianCities)
        {
            if (!existingCities.Contains(slug))
            {
                db.Cities.Add(new City { CountryId = romania.Id, Slug = slug, Name = name, IsActive = true });
            }
        }

        await db.SaveChangesAsync();

        var cityIds = await db.Cities
            .Where(c => c.CountryId == romania.Id)
            .ToDictionaryAsync(c => c.Slug, c => c.Id);

        var existingNeighborhoods = await db.Neighborhoods
            .Select(n => new { n.CityId, n.Name })
            .ToListAsync();

        var known = existingNeighborhoods.Select(n => (n.CityId, n.Name)).ToHashSet();

        foreach (var (citySlug, names) in Neighborhoods)
        {
            if (!cityIds.TryGetValue(citySlug, out var cityId))
            {
                continue;
            }

            foreach (var name in names)
            {
                if (known.Add((cityId, name)))
                {
                    db.Neighborhoods.Add(new Neighborhood { CityId = cityId, Name = name, IsActive = true });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    public static async Task SeedFoodCategoriesAsync(ApplicationDbContext db)
    {
        var existing = await db.FoodCategories.ToDictionaryAsync(c => c.Key);
        var order = 0;

        foreach (var (key, name, description, isAllowed) in FoodCategories)
        {
            order++;

            if (existing.TryGetValue(key, out var category))
            {
                // Keep the allowlist authoritative: a category that became prohibited must not
                // stay usable just because it already exists in an older database.
                category.Name = name;
                category.Description = description;
                category.IsAllowed = isAllowed;
                category.SortOrder = order;
                continue;
            }

            db.FoodCategories.Add(new FoodCategory
            {
                Key = key,
                Name = name,
                Description = description,
                IsActive = true,
                IsAllowed = isAllowed,
                SortOrder = order
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedSampleDataAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string password,
        ILogger logger)
    {
        var cluj = await db.Cities.FirstOrDefaultAsync(c => c.Slug == "cluj-napoca");
        if (cluj is null)
        {
            return;
        }

        var marasti = await db.Neighborhoods.FirstOrDefaultAsync(n => n.CityId == cluj.Id && n.Name == "Mărăști");
        var gheorgheni = await db.Neighborhoods.FirstOrDefaultAsync(n => n.CityId == cluj.Id && n.Name == "Gheorgheni");

        var ioana = await EnsureUserAsync(userManager, DevUserOneEmail, "Ioana", "Popescu", "0722000111",
            cluj.CountryId, cluj.Id, marasti?.Id, password, logger);
        await EnsureUserAsync(userManager, DevUserTwoEmail, "Andrei", "Ionescu", "0733000222",
            cluj.CountryId, cluj.Id, gheorgheni?.Id, password, logger);

        if (ioana is null || await db.DonationItems.AnyAsync())
        {
            return;
        }

        var categories = await db.FoodCategories
            .Where(c => c.IsAllowed)
            .ToDictionaryAsync(c => c.Key, c => c.Id);

        var now = DateTime.UtcNow;
        var today = RoDate.Today;

        var samples = new (string Title, string CategoryKey, int ExpiresInDays, int? NeighborhoodId, string PickupLocation, string? PickupNotes)[]
        {
            ("Pâine integrală feliată", "PackagedBakery", 5, marasti?.Id,
                "Stația de tramvai Mărăști", "Zilnic după ora 18:00."),
            ("Paste penne integrale 500 g", "PackagedPantry", 240, marasti?.Id,
                "Intrarea Iulius Mall, dinspre Bulevardul 21 Decembrie", null),
            ("Suc natural de mere 1 l", "PackagedBeverages", 90, gheorgheni?.Id,
                "Parcul Iuliu Hațieganu, intrarea principală", "Weekend, între 10:00 și 14:00."),
            ("Mere ambalate 1 kg", "PackagedProduce", 8, gheorgheni?.Id,
                "Piața Mihai Viteazu, lângă intrare", null),
            ("Batoane cu ovăz și fructe", "PackagedSnacks", 120, null,
                "Piața Unirii, lângă statuia lui Matei Corvin", "Pot lăsa pachetul și la birou, în centru."),
            ("Orez cu bob lung 1 kg", "PackagedPantry", 400, marasti?.Id,
                "Stația de autobuz Aurel Vlaicu", null),
            ("Biscuiți digestivi", "PackagedBakery", 60, null,
                "Gara Cluj-Napoca, intrarea principală", "Doar în timpul săptămânii, dimineața.")
        };

        var index = 0;
        foreach (var (title, categoryKey, expiresInDays, neighborhoodId, pickupLocation, pickupNotes) in samples)
        {
            if (!categories.TryGetValue(categoryKey, out var categoryId))
            {
                continue;
            }

            db.DonationItems.Add(new DonationItem
            {
                Title = title,
                FoodCategoryId = categoryId,
                ExpirationDate = today.AddDays(expiresInDays),
                CountryId = cluj.CountryId,
                CityId = cluj.Id,
                NeighborhoodId = neighborhoodId,
                PickupLocation = pickupLocation,
                PickupNotes = pickupNotes,
                Status = DonationStatus.Available,
                DonatorId = ioana.Id,
                SafetyConfirmedAt = now,
                CreatedAt = now.AddHours(-(++index * 5))
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Sample food donations seeded");
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        string phone,
        int countryId,
        int cityId,
        int? neighborhoodId,
        string password,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            PreferredCountryId = countryId,
            PreferredCityId = cityId,
            PreferredNeighborhoodId = neighborhoodId,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            return user;
        }

        logger.LogWarning("Could not create sample user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Code)));
        return null;
    }
}
