using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Data;

public static class DbSeeder
{
    public const string DevDonatorEmail = "donator@example.local";
    public const string DevReceiverEmail = "receiver@example.local";

    public static readonly (string Key, string Name)[] Categories =
    {
        ("Furniture", "Mobilier"),
        ("Electronics", "Electronice"),
        ("Kitchen", "Bucătărie"),
        ("Clothing", "Îmbrăcăminte"),
        ("Books", "Cărți"),
        ("Toys", "Jucării"),
        ("BabyChildren", "Bebeluși și copii"),
        ("HomeGarden", "Casă și grădină"),
        ("Sports", "Sport"),
        ("Tools", "Unelte"),
        ("Other", "Altele")
    };

    /// <summary>Applies migrations (optional) and seeds roles, categories and — optionally — development data.</summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));
        var db = provider.GetRequiredService<ApplicationDbContext>();

        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await WaitForDatabaseServerAsync(db, logger);

            if (db.Database.GetMigrations().Any())
            {
                logger.LogInformation("Applying database migrations");
                await db.Database.MigrateAsync();
            }
            else
            {
                // No migration files in the project yet (e.g. first Docker run):
                // create the schema directly from the model.
                logger.LogWarning("No EF Core migrations found; creating the database schema with EnsureCreated");
                await db.Database.EnsureCreatedAsync();
            }
        }

        await SeedRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole>>());
        await SeedCategoriesAsync(db);

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

    /// <summary>
    /// SQL Server in Docker can take a little while to accept connections; retry for up to ~2 minutes.
    /// Connects to the server (master) so it works before the application database exists.
    /// </summary>
    private static async Task WaitForDatabaseServerAsync(ApplicationDbContext db, ILogger logger)
    {
        var connectionString = db.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString) || !db.Database.IsSqlServer())
        {
            return;
        }

        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 5
        };

        const int maxAttempts = 24;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                return;
            }
            catch (Microsoft.Data.SqlClient.SqlException) when (attempt < maxAttempts)
            {
                logger.LogInformation("Waiting for SQL Server to become available (attempt {Attempt}/{Max})", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedCategoriesAsync(ApplicationDbContext db)
    {
        var existing = await db.Categories.Select(c => c.Key).ToListAsync();
        var order = 0;
        foreach (var (key, name) in Categories)
        {
            order++;
            if (!existing.Contains(key))
            {
                db.Categories.Add(new Category { Key = key, Name = name, SortOrder = order });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedSampleDataAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string password,
        ILogger logger)
    {
        var donator = await EnsureUserAsync(userManager, DevDonatorEmail, "Ioana", "Popescu", "0722000111", AccountType.Donator, password, logger);
        await EnsureUserAsync(userManager, DevReceiverEmail, "Andrei", "Ionescu", "0733000222", AccountType.Receiver, password, logger);

        if (donator is null || await db.DonationItems.AnyAsync())
        {
            return;
        }

        var categories = await db.Categories.ToDictionaryAsync(c => c.Key, c => c.Id);
        var now = DateTime.UtcNow;

        var samples = new[]
        {
            ("Scaun de birou", "Scaun de birou ergonomic, reglabil pe înălțime, cu spătar din plasă. Are câteva urme de uzură pe cotiere, dar funcționează perfect.", "Furniture", ProductCondition.Good, "Cluj-Napoca, Mărăști"),
            ("Set de farfurii", "Set de 12 farfurii din porțelan alb (6 întinse și 6 adânci). Nu sunt ciobite și se pot spăla în mașina de spălat vase.", "Kitchen", ProductCondition.LikeNew, "București, Sector 3"),
            ("Cărți pentru copii", "Colecție de 15 cărți ilustrate pentru copii între 3 și 7 ani, în limba română. Paginile sunt întregi.", "Books", ProductCondition.Good, "Iași, Copou"),
            ("Lampă de birou", "Lampă de birou LED cu braț flexibil și trei trepte de intensitate. Vine cu alimentatorul original.", "Electronics", ProductCondition.LikeNew, "Timișoara, Centru"),
            ("Geacă de iarnă", "Geacă de iarnă pentru bărbați, mărimea L, culoare bleumarin. Călduroasă, cu glugă detașabilă.", "Clothing", ProductCondition.Used, "Brașov, Tractorul"),
            ("Jucării pentru copii", "Cutie cu jucării diverse: cuburi de construcție, mașinuțe și un puzzle de 50 de piese. Toate piesele sunt curate.", "Toys", ProductCondition.Good, "Cluj-Napoca, Gheorgheni"),
            ("Masă mică de bucătărie", "Masă din lemn de 80 x 60 cm, potrivită pentru o bucătărie mică. Un picior are nevoie de o strângere de șurub.", "Furniture", ProductCondition.NeedsRepair, "Sibiu, Vasile Aaron")
        };

        var index = 0;
        foreach (var (title, description, categoryKey, condition, area) in samples)
        {
            db.DonationItems.Add(new DonationItem
            {
                Title = title,
                Description = description,
                CategoryId = categories[categoryKey],
                Condition = condition,
                PickupArea = area,
                Status = DonationStatus.Available,
                DonatorId = donator.Id,
                CreatedAt = now.AddHours(-(++index * 5))
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Sample donations seeded");
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        string phone,
        AccountType accountType,
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
            AccountType = accountType,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Could not create sample user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Code)));
            return null;
        }

        var role = accountType == AccountType.Donator ? AppRoles.Donator : AppRoles.Receiver;
        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
