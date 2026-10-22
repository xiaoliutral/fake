using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Dashboard 可选订阅拓扑（如 EventBus Topology）。未注册则不展示。
/// </summary>
public interface IMessagingDashboardSubscriptionsProvider
{
    IReadOnlyList<MessagingSubscriptionDto> GetSubscriptions();
}
