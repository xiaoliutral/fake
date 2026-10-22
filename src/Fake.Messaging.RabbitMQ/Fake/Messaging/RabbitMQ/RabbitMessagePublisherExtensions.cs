namespace Fake.Messaging.RabbitMQ;

public static class RabbitMessagePublisherExtensions
{
    public static Task PublishAsync(
        this IMessagePublisher publisher,
        RabbitEnvelope envelope,
        CancellationToken cancellationToken = default)
        => publisher.PublishAsync(envelope.ToMessageEnvelope(), cancellationToken);
}
