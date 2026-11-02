namespace AmeWatches.Api.Models.Payments;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    public string Provider { get; set; } = "Simulator";
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
    public string? WebhookSecret { get; set; }
    public int ExpiryMinutes { get; set; } = 30;
    public int WebhookToleranceSeconds { get; set; } = 300;
}
