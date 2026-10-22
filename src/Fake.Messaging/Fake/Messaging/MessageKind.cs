namespace Fake.Messaging;

/// <summary>
/// 消息通道分类（便于 Dashboard 筛选与投递分流）。
/// </summary>
public enum MessageKind
{
    EventBus = 0,
    Rabbit = 1
}
