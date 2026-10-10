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

        context.Services.AddHostedService<OutboxPublisherBackgroundService>();
        context.Services.AddHostedService<InboxCleanupBackgroundService>();
    }
}
