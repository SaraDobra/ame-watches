using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AmeWatches.Api.Models.Auth;
using AmeWatches.Api.Models.Email;
using AmeWatches.Api.Services.Email;
using AmeWatches.Api.Models.Payments;
using AmeWatches.Api.Models.Storage;
using AmeWatches.Api.Services.Storage;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Auth;
using AmeWatches.Api.Services.Payments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
if (emailOptions.Mode.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(emailOptions.Host))
        throw new InvalidOperationException("Email:Mode=Smtp kërkon Email:Host.");
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("Në prodhim Email:Mode duhet të jetë Smtp.");
}
else
{
    builder.Services.AddSingleton<IEmailSender, FileEmailSender>();
}
builder.Services.AddSingleton<EmailOutbox>();
builder.Services.AddSingleton<IEmailOutbox>(sp => sp.GetRequiredService<EmailOutbox>());
builder.Services.AddSingleton<OrderEmailBuilder>();
builder.Services.AddSingleton<IOrderNotifier, OrderNotifier>();
builder.Services.AddHostedService<EmailDispatchService>();

builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
var paymentOptions = builder.Configuration.GetSection(PaymentOptions.SectionName).Get<PaymentOptions>() ?? new PaymentOptions();
if (paymentOptions.Provider.Equals("Simulator", StringComparison.OrdinalIgnoreCase))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("Simulatori i pagesës nuk lejohet në prodhim. Vendos Payments:Provider te një ofrues real.");

    builder.Services.AddSingleton<SimulatorPaymentProvider>();
    builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<SimulatorPaymentProvider>());
}
else
{
    throw new InvalidOperationException($"Ofruesi i pagesës '{paymentOptions.Provider}' nuk njihet.");
}
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHostedService<PaymentExpiryService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("orders", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:SecretKey mungon ose është më i shkurtër se 32 karaktere.");

    jwtOptions.SecretKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
builder.Services.Configure<JwtOptions>(o =>
{
    o.SecretKey = jwtOptions.SecretKey;
    o.Issuer = jwtOptions.Issuer;
    o.Audience = jwtOptions.Audience;
    o.ExpiresMinutes = jwtOptions.ExpiresMinutes;
});
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));

builder.Services.AddIdentityCore<AppUser>(options =>
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<LocalImageStorage>();
builder.Services.AddSingleton<IImageStorage>(sp => sp.GetRequiredService<LocalImageStorage>());
builder.Services.AddScoped<IAdminImageService, AdminImageService>();
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await SeedData.InitializeAsync(app.Services);
}

await AdminSeeder.EnsureAdmin(app.Services);

var uploads = app.Services.GetRequiredService<LocalImageStorage>();
Directory.CreateDirectory(uploads.Root);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploads.Root),
    RequestPath = "/uploads",
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
});

app.UseCors("AllowAngularDev");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
