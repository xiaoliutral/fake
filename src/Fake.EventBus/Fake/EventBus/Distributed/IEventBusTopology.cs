namespace Fake.EventBus.Distributed;

/// <summary>
/// 事件拓扑：当前进程已注册的集成事件与消费者。
/// </summary>
public interface IEventBusTopology
{
    IReadOnlyList<EventSubscriptionDto> GetSubscriptions();
}
