namespace Fake.EventBus.Distributed;

public interface IEventRoutingKeyResolver
{
    string Resolve(Type eventType);
}
