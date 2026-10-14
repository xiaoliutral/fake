using Fake.DependencyInjection;
using Fake.EventBus.Distributed;
using Fake.EventBus.Local;
using Fake.Modularity;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus;

[DependsOn(typeof(FakeUnitOfWorkModule))]
public class FakeEventBusModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnServiceExposing(exposingContext =>
        {
            foreach (var interfaceType in exposingContext.ImplementationType.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                {
                    exposingContext.ExposedServices.TryAdd(new ServiceIdentifier(interfaceType));
                }
            }
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<EventRoutingOptions>(configuration.GetSection(EventRoutingOptions.SectionName));
        context.Services.TryAddSingleton<IEventRoutingKeyResolver, DefaultEventRoutingKeyResolver>();
        context.Services.TryAddSingleton<EventBusMetrics>();

        // 同一实例：IEventBus / ILocalEventBus 均指向本地总线
        context.Services.AddSingleton<LocalEventBus>();
        context.Services.AddSingleton<ILocalEventBus>(sp => sp.GetRequiredService<LocalEventBus>());
        context.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<LocalEventBus>());

        // 默认空实现；接入 Fake.EventBus.IntegrationEventLog / RabbitMQ 后由对应模块 Replace
        context.Services.TryAddTransient<IOutboxEventLogService>(_ => NullOutboxEventLogService.Instance);
        context.Services.TryAddTransient<IInboxEventLogService>(_ => NullInboxEventLogService.Instance);
        context.Services.TryAddTransient<IOutboxEventPublisher>(_ => NullOutboxEventPublisher.Instance);
        context.Services.TryAddTransient<IOutboxAdminStore>(_ => NullOutboxAdminStore.Instance);
        context.Services.TryAddTransient<IInboxAdminStore>(_ => NullInboxAdminStore.Instance);
        context.Services.TryAddSingleton<IDlxAdminService>(_ => NullDlxAdminService.Instance);
        context.Services.TryAddSingleton<IEventBusTopology>(_ => NullEventBusTopology.Instance);
        context.Services.TryAddTransient<IInboxEventReprocessor>(_ => NullInboxEventReprocessor.Instance);
    }
}
