namespace Fake.EventBus.Distributed;

/// <summary>
/// 分布式消息 Header 约定（AMQP / 其他传输共用 key）。
/// </summary>
public static class EventBusMessageHeaders
{
    public const string EventId = "fake-event-id";
    public const string EventType = "fake-event-type";
}
