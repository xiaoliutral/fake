namespace Fake.Messaging;

/// <summary>
/// 可靠 / 即时消息发布。用法：<c>await publisher.PublishAsync(message);</c>
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// 按 <see cref="MessageRouteAttribute"/> 发布。
    /// 默认经 Outbox；<see cref="MessageRouteAttribute.Immediate"/> 时立即投递。
    /// delivery MessageId 新建；业务 <see cref="Message.Id"/> 写入 EventId header。
    /// </summary>
    Task PublishAsync(Message message, CancellationToken cancellationToken = default);
}
