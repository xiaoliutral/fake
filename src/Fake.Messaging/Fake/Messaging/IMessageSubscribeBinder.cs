namespace Fake.Messaging;

/// <summary>
/// 将 <see cref="IMessageHandle{TMessage}"/> 订阅描述绑定到具体传输（Rabbit / Kafka / …）。
/// </summary>
public interface IMessageSubscribeBinder
{
    /// <summary>传输标识，如 <c>rabbit</c>；与 <see cref="MessageSubscribeAttribute.Provider"/> 对应。</summary>
    string Provider { get; }

    void Bind(MessageSubscribeDescriptor descriptor);
}
