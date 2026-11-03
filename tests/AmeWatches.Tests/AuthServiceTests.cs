using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Auth;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace AmeWatches.Tests;

public class AuthServiceTests
{
    private static async Task<(AuthTestKit kit, IAuthService auth)> Setup()
    {
        var kit = new AuthTestKit();
        await kit.SeedAdmin();
        return (kit, kit.Services.CreateScope().ServiceProvider.GetRequiredService<IAuthService>());
    }

    private static LoginRequest Login(string email, string password) => new(email, password);

    [Fact]
    public async Task CorrectCredentials_ReturnAToken()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;

        var result = await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword));

        Assert.Null(result.Failure);
        Assert.False(string.IsNullOrWhiteSpace(result.Success!.Token));
        Assert.Equal("Admin Test", result.Success.FullName);
        Assert.Equal(kit.Time.Now.UtcDateTime.AddMinutes(480), result.Success.ExpiresAt);
    }

    [Fact]
    public async Task WrongPasswordAndUnknownEmail_GiveTheSameAnswer()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;

        var wrongPassword = await auth.Login(Login(AuthTestKit.AdminEmail, "gabim-gabim-1"));
        var unknownEmail = await auth.Login(Login("askush@ame.test", "gabim-gabim-1"));

        Assert.Equal(LoginFailure.InvalidCredentials, wrongPassword.Failure);
        Assert.Equal(LoginFailure.InvalidCredentials, unknownEmail.Failure);
    }

    [Fact]
    public async Task AfterFiveWrongAttempts_EvenTheCorrectPasswordIsRefused()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;

        for (var i = 0; i < 5; i++)
            await auth.Login(Login(AuthTestKit.AdminEmail, "gabim-gabim-1"));

        var correct = await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword));

        Assert.Equal(LoginFailure.LockedOut, correct.Failure);
    }

    [Fact]
    public async Task ASuccessfulLogin_ResetsTheFailureCounter()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;

        for (var i = 0; i < 4; i++)
            await auth.Login(Login(AuthTestKit.AdminEmail, "gabim-gabim-1"));
        await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword));
        for (var i = 0; i < 4; i++)
            await auth.Login(Login(AuthTestKit.AdminEmail, "gabim-gabim-1"));

        var result = await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword));

        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task AUserWithoutTheAdminRole_CannotLogIn()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        using (var scope = kit.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            await users.CreateAsync(new AppUser { UserName = "klient@ame.test", Email = "klient@ame.test", FullName = "Klient" }, "FjalekalimiIMire1");
        }

        var result = await auth.Login(Login("klient@ame.test", "FjalekalimiIMire1"));

        Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
    }

    [Fact]
    public async Task SeedingTwice_DoesNotOverwriteAnExistingPassword()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        using (var scope = kit.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = (await users.FindByEmailAsync(AuthTestKit.AdminEmail))!;
            await users.RemovePasswordAsync(admin);
            await users.AddPasswordAsync(admin, "FjalekalimiITjeter9");
        }

        await kit.SeedAdmin();

        Assert.Null((await auth.Login(Login(AuthTestKit.AdminEmail, "FjalekalimiITjeter9"))).Failure);
        Assert.Equal(LoginFailure.InvalidCredentials, (await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword))).Failure);
    }

    [Fact]
    public async Task WithoutAdminConfiguration_NoUserIsCreated()
    {
        await using var kit = new AuthTestKit(adminEmail: null, adminPassword: null);

        await kit.SeedAdmin();

        using var scope = kit.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().Users.ToList());
    }

    private static TokenValidationParameters Parameters(AuthTestKit kit, string secret = AuthTestKit.Secret) => new()
    {
        ValidIssuer = kit.Jwt.Issuer,
        ValidAudience = kit.Jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateLifetime = false,
        ClockSkew = TimeSpan.Zero
    };

    [Fact]
    public async Task TheToken_IsValidAndCarriesTheAdminRole()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        var token = (await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword))).Success!.Token;

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters(kit));

        Assert.True(validation.IsValid);
        Assert.Contains(validation.ClaimsIdentity.Claims, c => c.Type == ClaimTypes.Role && c.Value == Roles.Admin);
    }

    [Fact]
    public async Task ATokenSignedWithAnotherKey_IsRejected()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        var token = (await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword))).Success!.Token;

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters(kit, "another-secret-key-another-secret-key-1"));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task ATamperedToken_IsRejected()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        var token = (await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword))).Success!.Token;
        var parts = token.Split('.');
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1])).Replace("\"Admin\"", "\"Root\"");
        var tampered = $"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}";

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(tampered, Parameters(kit));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task AnExpiredToken_IsRejected()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        kit.Time.Now = DateTimeOffset.UtcNow.AddDays(-2);
        var token = (await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword))).Success!.Token;
        var parameters = Parameters(kit);
        parameters.ValidateLifetime = true;

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task AFreshToken_PassesTheLifetimeCheck()
    {
        var (kit, auth) = await Setup();
        await using var _ = kit;
        kit.Time.Now = DateTimeOffset.UtcNow;
        var token = (await auth.Login(Login(AuthTestKit.AdminEmail, AuthTestKit.AdminPassword))).Success!.Token;
        var parameters = Parameters(kit);
        parameters.ValidateLifetime = true;

        Assert.True((await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters)).IsValid);
    }
}
