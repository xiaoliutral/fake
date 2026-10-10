using Fake.EntityFrameworkCore;
using Fake.EventBus;
using Fake.EventBus.Distributed;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus.IntegrationEventLog;

[DependsOn(
    typeof(FakeEventBusModule),
    typeof(FakeEntityFrameworkCoreModule))]
public class FakeEventBusIntegrationEventLogModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddOptions<OutboxPublisherOptions>()
            .BindConfiguration(OutboxPublisherOptions.SectionName)
            .ValidateOnStart();

        context.Services.AddOptions<InboxCleanupOptions>()
            .BindConfiguration(InboxCleanupOptions.SectionName)
            .ValidateOnStart();

        context.Services.Replace(
            ServiceDescriptor.Transient<IOutboxEventLogService, OutboxEventLogService>());
        context.Services.Replace(
            ServiceDescriptor.Transient<IInboxEventLogService, InboxEventLogService>());
        context.Services.Replace(
            ServiceDescriptor.Transient<IOutboxEventPublisher, OutboxEventPublisher>());

        // 业务注入 IDistributedEventBus → Outbox；解析时校验物理总线已注册（如 RabbitMQ）
        context.Services.Replace(ServiceDescriptor.Singleton<IDistributedEventBus>(sp =>
        {
            _ = sp.GetService<IPhysicalDistributedEventBus>()
                ?? throw new InvalidOperationException(
                    "Outbox 需要 IPhysicalDistributedEventBus。请依赖 FakeEventBusRabbitMqModule，或自行注册物理总线。");
            return ActivatorUtilities.CreateInstance<OutboxDistributedEventBus>(sp);
        }));

        context.Services.AddHostedService<OutboxPublisherBackgroundService>();
        context.Services.AddHostedService<InboxCleanupBackgroundService>();
    }
}
