using Fake.EventBus.IntegrationEventLog.Tests.Events;

namespace Fake.EventBus.IntegrationEventLog.Tests.Fakes;

public class TrackingInboxHandler : IEventHandler<TestOutboxIntegrationEvent>
{
    public static readonly List<Guid> Handled = new();

    public static void Reset() => Handled.Clear();

    public Task HandleAsync(TestOutboxIntegrationEvent @event, CancellationToken cancellationToken)
    {
        Handled.Add(@event.Id);
        return Task.CompletedTask;
    }
}
