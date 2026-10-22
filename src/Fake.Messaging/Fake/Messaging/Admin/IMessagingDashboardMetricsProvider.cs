using Fake.Messaging;

namespace Fake.Messaging.Admin;

/// <summary>
/// Dashboard 可选指标源（如 EventBus Metrics）。未注册则不展示。
/// </summary>
public interface IMessagingDashboardMetricsProvider
{
    object GetSnapshot();
}
