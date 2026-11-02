using AmeWatches.Api.Data;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AmeWatches.Api.Models.Payments;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IOrderService, OrderService>();

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
    options.AddPolicy("orders", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseCors("AllowAngularDev");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
