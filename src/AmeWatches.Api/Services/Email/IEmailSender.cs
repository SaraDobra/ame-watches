using AmeWatches.Api.Models.Email;

namespace AmeWatches.Api.Services.Email;

public interface IEmailSender
{
    Task Send(EmailMessage message, CancellationToken cancellationToken);
}
