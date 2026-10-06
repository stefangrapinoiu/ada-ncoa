using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace DaMaiDeparte.Web.Monitoring;

/// <summary>
/// Server-side command to grant or revoke the Admin role, run inside the container:
/// <c>docker compose exec web dotnet DaMaiDeparte.Web.dll --grant-admin someone@example.com</c>.
/// Only existing accounts can be promoted — there is no way to become admin from the web UI,
/// and no e-mail list in config (registration doesn't verify e-mail, so a listed address could
/// be registered by someone else first).
/// </summary>
public static class AdminCommands
{
    /// <returns>null when no admin command was given; otherwise the process exit code.</returns>
    public static async Task<int?> TryRunAsync(IServiceProvider services, string[] args, TextWriter output)
    {
        var grantIndex = Array.IndexOf(args, "--grant-admin");
        var revokeIndex = Array.IndexOf(args, "--revoke-admin");
        if (grantIndex < 0 && revokeIndex < 0)
        {
            return null;
        }

        var grant = grantIndex >= 0;
        var index = grant ? grantIndex : revokeIndex;
        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            output.WriteLine("Folosire: --grant-admin <email> sau --revoke-admin <email>");
            return 1;
        }

        var email = args[index + 1].Trim();

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roles.RoleExistsAsync(AdminRole.Name))
        {
            await roles.CreateAsync(new IdentityRole(AdminRole.Name));
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            output.WriteLine($"Nu există niciun cont cu adresa {email}. Contul trebuie creat întâi în aplicație.");
            return 1;
        }

        var isAdmin = await users.IsInRoleAsync(user, AdminRole.Name);
        IdentityResult result;
        if (grant)
        {
            result = isAdmin ? IdentityResult.Success : await users.AddToRoleAsync(user, AdminRole.Name);
        }
        else
        {
            result = isAdmin ? await users.RemoveFromRoleAsync(user, AdminRole.Name) : IdentityResult.Success;
        }

        if (!result.Succeeded)
        {
            output.WriteLine("Eroare: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            return 1;
        }

        output.WriteLine(grant
            ? $"Rolul Admin a fost acordat pentru {email}. Persoana trebuie să se delogheze și să se logheze din nou."
            : $"Rolul Admin a fost retras pentru {email}.");
        return 0;
    }
}
