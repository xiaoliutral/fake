using System.Collections.Concurrent;
using Fake.EventBus.Distributed;

namespace Fake.EventBus.IntegrationEventLog.Tests.Fakes;

public sealed class FakePhysicalDistributedEventBus : IPhysicalDistributedEventBus
{
    private readonly ConcurrentQueue<IntegrationEvent> _published = new();

    public IReadOnlyList<IntegrationEvent> Published => _published.ToArray();

    public void Clear() => _published.Clear();

    public Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _published.Enqueue(@event);
        return Task.CompletedTask;
    }

    public Task PublishAsync(Event @event, CancellationToken cancellationToken = default)
        => PublishAsync((IntegrationEvent)@event, cancellationToken);
}
