namespace AmeWatches.Api.Models.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string? SecretKey { get; set; }
    public string Issuer { get; set; } = "ame-watches-api";
    public string Audience { get; set; } = "ame-watches-admin";
    public int ExpiresMinutes { get; set; } = 480;
}

public class AdminSeedOptions
{
    public const string SectionName = "Admin";

    public string? Email { get; set; }
    public string? Password { get; set; }
    public string FullName { get; set; } = "Administratori";
}
