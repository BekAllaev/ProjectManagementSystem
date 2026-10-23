using System.Threading.Channels;

namespace ProjectManagementSystem.Infrastructure.Outbox;

/// <summary>
/// Wakes the outbox processor after a commit that added messages. Holds at most one signal: signals sent while the
/// processor is busy collapse into one and are not lost, so the processor reads the outbox once more afterwards.
/// </summary>
public sealed class OutboxSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(CancellationToken cancellationToken) =>
        await _channel.Reader.ReadAsync(cancellationToken);
}
