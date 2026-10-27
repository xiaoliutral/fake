using Microsoft.Extensions.DependencyInjection;

namespace Fake.Messaging;

/// <summary>
/// 无 Outbox：直接经 <see cref="IPhysicalMessageTransport"/> 投递。
/// 生产环境由 <c>Fake.Messaging.EntityFrameworkCore</c> 的 <c>ReliableMessagePublisher</c> 覆盖。
/// </summary>
public sealed class DirectMessagePublisher(
    IServiceProvider serviceProvider,
    IMessageBodySerializer serializer,
    IMessageRouteResolver routeResolver) : IMessagePublisher
{
    public Task PublishAsync(Message message, CancellationToken cancellationToken = default)
    {
        var envelope = MessageEnvelopeFactory.Create(message, routeResolver, serializer);
        return PublishPhysicalAsync(envelope, cancellationToken);
    }

    private async Task PublishPhysicalAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var transports = serviceProvider.GetServices<IPhysicalMessageTransport>().ToList();
        var transport = transports.FirstOrDefault(t => t.CanPublish(envelope.Destination))
                        ?? throw new InvalidOperationException(
                            $"No IPhysicalMessageTransport for destination '{envelope.Destination}'.");
        await transport.PublishAsync(envelope, cancellationToken);
    }
}
