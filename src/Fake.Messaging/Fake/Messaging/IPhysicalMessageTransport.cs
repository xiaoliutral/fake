namespace Fake.Messaging;

/// <summary>
/// Outbox Dispatcher 调用的物理投递适配器（按 Destination 前缀选择）。
/// </summary>
public interface IPhysicalMessageTransport
{
    bool CanPublish(string destination);

    Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);
}
