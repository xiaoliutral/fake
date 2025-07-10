namespace Fake.EventBus.Distributed;

public interface IDistributedEventBus : IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="event">事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken);

    Task IEventBus.PublishAsync(Event @event, CancellationToken cancellationToken) =>
        PublishAsync((IntegrationEvent)@event, cancellationToken);
}