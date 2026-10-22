namespace Fake.Messaging;

/// <summary>
/// 独立队列可靠发布（经 Outbox）。
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);
}
