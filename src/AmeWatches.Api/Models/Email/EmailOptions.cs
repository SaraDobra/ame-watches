namespace AmeWatches.Api.Models.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Mode { get; set; } = "File";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "ame.accessories.ks@gmail.com";
    public string FromName { get; set; } = "amé watches";
    public string ShopAddress { get; set; } = "ame.accessories.ks@gmail.com";
    public string PublicBaseUrl { get; set; } = "http://localhost:4200";
    public string? SinkFolder { get; set; }
}

public record EmailMessage(
    string To,
    string? ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? ReplyTo = null);
