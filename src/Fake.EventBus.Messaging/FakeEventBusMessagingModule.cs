using Fake.EventBus;
using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.Messaging.Admin;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus.Messaging;

[DependsOn(
    typeof(FakeEventBusModule),
    typeof(FakeMessagingEntityFrameworkCoreModule))]
public class FakeEventBusMessagingModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Replace(
            ServiceDescriptor.Transient<IOutboxEventPublisher, OutboxEventPublisher>());

        // Messaging Dashboard 可选适配（有则显示 Metrics / Inbox 本地 Reprocess）
        context.Services.TryAddSingleton<IMessagingDashboardMetricsProvider, EventBusMetricsDashboardAdapter>();
        context.Services.TryAddTransient<IMessagingInboxLocalReprocessor, InboxEventReprocessor>();

        context.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IPhysicalMessageTransport, EventBusPhysicalMessageTransport>());

        context.Services.Replace(ServiceDescriptor.Singleton<IDistributedEventBus>(sp =>
        {
            _ = sp.GetService<IPhysicalDistributedEventBus>()
                ?? throw new InvalidOperationException(
                    "Outbox 需要 IPhysicalDistributedEventBus。请依赖 FakeEventBusRabbitMqModule，或自行注册物理总线。");
            return ActivatorUtilities.CreateInstance<OutboxDistributedEventBus>(sp);
        }));
    }
}
