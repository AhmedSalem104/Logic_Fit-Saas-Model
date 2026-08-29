using System.Threading.Channels;
using LogicFit.Application;

namespace LogicFit.Infrastructure.Provisioning;

public sealed class ProvisioningQueue : IProvisioningQueue
{
    private readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    public ValueTask EnqueueAsync(Guid operationId, CancellationToken cancellationToken = default)
        => channel.Writer.WriteAsync(operationId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken = default)
        => channel.Reader.ReadAllAsync(cancellationToken);
}
