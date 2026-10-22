using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.Messaging.Admin;

namespace Fake.EventBus.Messaging;

public sealed class EventBusMetricsDashboardAdapter(EventBusMetrics metrics) : IMessagingDashboardMetricsProvider
{
    public object GetSnapshot() => metrics.GetSnapshot();
}
