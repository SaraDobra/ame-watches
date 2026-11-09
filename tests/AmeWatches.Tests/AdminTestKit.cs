using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Auth;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AmeWatches.Tests;

public sealed class AuthTestKit : IAsyncDisposable
{
    public const string AdminEmail = "admin@ame.test";
    public const string AdminPassword = "FjalekalimiIMire1";
    public const string Secret = "0123456789abcdef0123456789abcdef-test";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ServiceProvider Services { get; }
    public TestTimeProvider Time { get; } = new();
    public JwtOptions Jwt { get; } = new() { SecretKey = Secret };

    public AuthTestKit(string? adminEmail = AdminEmail, string? adminPassword = AdminPassword)
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(Microsoft.Extensions.Logging.Abstractions.NullLoggerProvider.Instance));
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddSingleton<TimeProvider>(Time);
        services.Configure<JwtOptions>(o => { o.SecretKey = Jwt.SecretKey; o.Issuer = Jwt.Issuer; o.Audience = Jwt.Audience; o.ExpiresMinutes = Jwt.ExpiresMinutes; });
        services.Configure<AdminSeedOptions>(o => { o.Email = adminEmail; o.Password = adminPassword; o.FullName = "Admin Test"; });
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public Task SeedAdmin() => AdminSeeder.EnsureAdmin(Services);

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
