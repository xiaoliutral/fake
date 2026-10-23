namespace Fake.Messaging;

/// <summary>
/// 消息处理器（对齐 EventBus <c>IEventHandler&lt;T&gt;</c>）。
/// 一个类可实现多个 <c>IMessageHandle&lt;T&gt;</c>。
/// </summary>
public interface IMessageHandle<in TMessage> where TMessage : Message
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
