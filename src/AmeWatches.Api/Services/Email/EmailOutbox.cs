using System.Threading.Channels;
using AmeWatches.Api.Models.Email;

namespace AmeWatches.Api.Services.Email;

public interface IEmailOutbox
{
    bool Enqueue(EmailMessage message);
}

public class EmailOutbox : IEmailOutbox
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public bool Enqueue(EmailMessage message) => _channel.Writer.TryWrite(message);
}
