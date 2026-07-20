using Fake.DependencyInjection;
using Fake.EventBus.RabbitMQ.Tests.Events;

namespace Fake.EventBus.RabbitMQ.Tests.Events;

public class SimpleIntegrationEventHandler : IEventHandler<SimpleIntegrationEvent>, ITransientDependency
{
    private static int _handleCount;

    public static int HandleCount => _handleCount;

    public static void Init() => Interlocked.Exchange(ref _handleCount, 0);

    public Task HandleAsync(SimpleIntegrationEvent @event, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _handleCount);
        return Task.CompletedTask;
    }
}
