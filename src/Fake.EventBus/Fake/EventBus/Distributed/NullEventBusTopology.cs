namespace Fake.EventBus.Distributed;

public sealed class NullEventBusTopology : IEventBusTopology
{
    public static NullEventBusTopology Instance { get; } = new();

    public IReadOnlyList<EventSubscriptionDto> GetSubscriptions() => [];
}
