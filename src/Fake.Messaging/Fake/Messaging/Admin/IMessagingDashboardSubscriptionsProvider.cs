using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Dashboard 可选订阅拓扑。可注册多个（EventBus + Messaging 独立队列），API 合并返回。
/// </summary>
public interface IMessagingDashboardSubscriptionsProvider
{
    IReadOnlyList<MessagingSubscriptionDto> GetSubscriptions();
}
