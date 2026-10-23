using Fake.Messaging.Admin;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// Dashboard：从 <see cref="MessageSubscribeFinder"/> 汇总独立队列订阅。
/// </summary>
public sealed class RabbitMessageSubscriptionTopology(MessageSubscribeFinder finder)
    : IMessagingDashboardSubscriptionsProvider
{
    public IReadOnlyList<MessagingSubscriptionDto> GetSubscriptions()
    {
        return finder.FindAll()
            .OrderBy(d => d.Topic, StringComparer.Ordinal)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .Select(d => new MessagingSubscriptionDto(
                d.MessageType.FullName ?? d.MessageType.Name,
                d.MessageType.Name,
                string.IsNullOrEmpty(d.Topic) ? d.Name : $"{d.Topic}/{d.Name}",
                [d.HandlerType.FullName ?? d.HandlerType.Name]))
            .ToList();
    }
}
