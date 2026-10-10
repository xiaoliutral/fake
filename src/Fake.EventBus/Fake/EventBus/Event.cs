namespace Fake.EventBus;

[Serializable]
public abstract class Event
{
    /// <summary>
    /// 事件Id（init 以便 Outbox JSON 反序列化还原）
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 事件创建时间
    /// </summary>
    public virtual DateTime CreationTime { get; init; } = DateTime.Now;

    public override string ToString()
    {
        return $"[事件：{GetType().Name} Id：{Id} 创建时间：{CreationTime}]";
    }
}
