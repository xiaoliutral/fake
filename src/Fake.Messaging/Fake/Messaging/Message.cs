namespace Fake.Messaging;

/// <summary>
/// Messaging 消息基类（独立于 EventBus 的 <c>Event</c>）。
/// </summary>
[Serializable]
public abstract class Message
{
    /// <summary>消息 Id（init 以便 Outbox JSON 反序列化还原）。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>创建时间（UTC）。</summary>
    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 动态路由键；返回 null 时用 <see cref="MessageRouteAttribute.Name"/> 或类型名。
    /// </summary>
    public virtual string? GetRoutingKey() => null;

    public override string ToString()
        => $"[Message：{GetType().Name} Id：{Id} CreateAt：{CreationTime}]";
}
