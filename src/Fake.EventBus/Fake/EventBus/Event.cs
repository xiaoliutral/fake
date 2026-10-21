namespace Fake.EventBus;

[Serializable]
public abstract class Event
{
    /// <summary>
    /// 事件Id（init 以便 Outbox JSON 反序列化还原）
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 事件创建时间（UTC，与 Outbox/Inbox 及锁过期比较一致）
    /// </summary>
    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"[Name：{GetType().Name} Id：{Id} CreateAt：{CreationTime}]";
    }
}
