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
        // 同一实例：IEventBus / ILocalEventBus 均指向本地总线
        context.Services.AddSingleton<LocalEventBus>();
        context.Services.AddSingleton<ILocalEventBus>(sp => sp.GetRequiredService<LocalEventBus>());
        context.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<LocalEventBus>());

        // 默认空实现；接入 Fake.EventBus.IntegrationEventLog 后由该模块 Replace
        context.Services.TryAddTransient<IOutboxEventLogService>(_ => NullOutboxEventLogService.Instance);
        context.Services.TryAddTransient<IInboxEventLogService>(_ => NullInboxEventLogService.Instance);
    }
}
