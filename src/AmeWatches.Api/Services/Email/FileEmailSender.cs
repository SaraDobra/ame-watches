using System.Text;
using AmeWatches.Api.Models.Email;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Email;

public class FileEmailSender : IEmailSender
{
    private readonly string _folder;
    private readonly ILogger<FileEmailSender> _logger;

    public FileEmailSender(IOptions<EmailOptions> options, IWebHostEnvironment environment, ILogger<FileEmailSender> logger)
    {
        var configured = options.Value.SinkFolder;
        _folder = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "emails")
            : Path.GetFullPath(configured);
        _logger = logger;
    }

    public string Folder => _folder;

    public async Task Send(EmailMessage message, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_folder);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var safeTo = new string(message.To.Select(c => char.IsLetterOrDigit(c) || c is '@' or '.' or '-' ? c : '_').ToArray());
        var path = Path.Combine(_folder, $"{stamp}-{safeTo}.html");

        var header = new StringBuilder()
            .AppendLine("<!-- ")
            .AppendLine($"To: {message.To}")
            .AppendLine($"Reply-To: {message.ReplyTo}")
            .AppendLine($"Subject: {message.Subject}")
            .AppendLine("-->")
            .ToString();

        await File.WriteAllTextAsync(path, header + message.HtmlBody, cancellationToken);
        _logger.LogInformation("Email i ruajtur në {Path} (Mode=File). Për dërgim real vendos Email:Mode=Smtp.", path);
    }
}
