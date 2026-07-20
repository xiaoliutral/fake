using Fake.DependencyInjection;
using Fake.EventBus.Distributed;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus.RabbitMQ;

[DependsOn(typeof(FakeEventBusModule))]
[DependsOn(typeof(FakeRabbitMqModule))]
public class FakeEventBusRabbitMqModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnServiceExposing(exposingContext =>
        {
            foreach (var interfaceType in exposingContext.ImplementationType.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                {
                    var eventType = interfaceType.GenericTypeArguments[0];

                    // 暴露非 keyed 的 IEventHandler<T>，供 GetServices 解析订阅处理器
                    exposingContext.ExposedServices.TryAdd(new ServiceIdentifier(interfaceType));

                    context.Services.Configure<EventBusSubscriptionOptions>(o =>
                    {
                        // Keep track of all registered event types and their name mapping. We send these event types over the message bus
                        // and we don't want to do Type.GetType, so we keep track of the name mapping here.

                        // This list will also be used to subscribe to events from the underlying message broker implementation.
                        o.EventTypes[eventType.Name] = eventType;
                    });
                }
            }
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<RabbitMqEventBusOptions>(configuration.GetSection("RabbitMQ:EventBus"));

        // 同一实例同时充当 IEventBus / IDistributedEventBus / IHostedService
        context.Services.AddSingleton<RabbitMqEventBus>();
        context.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        context.Services.AddSingleton<IDistributedEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());
        context.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqEventBus>());
    }
}