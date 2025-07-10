namespace Fake.EventBus;

/// <summary>
/// 事件总线
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="event">事件</param>
    /// /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync(Event @event, CancellationToken cancellationToken = default);
}