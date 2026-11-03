using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Auth;

public static class AdminSeeder
{
    public static async Task EnsureAdmin(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var options = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
        var roles = sp.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var users = sp.GetRequiredService<UserManager<AppUser>>();

        if (!await roles.RoleExistsAsync(Roles.Admin))
            await roles.CreateAsync(new IdentityRole<int>(Roles.Admin));

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Nuk u krijua administrator: vendos Admin:Email dhe Admin:Password në konfigurim.");
            return;
        }

        if (await users.FindByEmailAsync(options.Email) is not null)
            return;

        var admin = new AppUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
            FullName = options.FullName,
            LockoutEnabled = true
        };

        var result = await users.CreateAsync(admin, options.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Krijimi i administratorit dështoi: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("U krijua administratori {Email}.", options.Email);
    }
}
