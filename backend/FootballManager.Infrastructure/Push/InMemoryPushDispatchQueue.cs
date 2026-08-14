using System.Threading.Channels;
using FootballManager.Application.Push;

namespace FootballManager.Infrastructure.Push;

public sealed class InMemoryPushDispatchQueue : IPushDispatchQueue
{
    private readonly Channel<PushWorkItem> _channel = Channel.CreateUnbounded<PushWorkItem>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(PushWorkItem item, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(item, cancellationToken);

    public IAsyncEnumerable<PushWorkItem> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
