namespace Fake.Messaging;

public interface IMessageOutbox
{
    Task EnqueueAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task MarkPublishFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}
