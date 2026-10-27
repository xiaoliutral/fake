using Fake.Messaging.Admin;
using Fake.Messaging.RabbitMQ;
using Fake.Modularity;
using Fake.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.Messaging;

/// <summary>
/// Rabbit 传输 + 独立队列消费。不含 ORM：Outbox/Inbox 由宿主另依赖 <see cref="FakeMessagingEntityFrameworkCoreModule"/>。
/// </summary>
[DependsOn(
    typeof(FakeMessagingModule),
    typeof(FakeRabbitMqModule))]
public class FakeMessagingRabbitMqModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddOptions<RabbitMessageOptions>()
            .Bind(configuration.GetSection(RabbitMessageOptions.SectionName));

        context.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IPhysicalMessageTransport, RabbitPhysicalMessageTransport>());

        context.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMessageSubscribeBinder, RabbitMessageSubscribeBinder>());

        context.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMessagingDashboardSubscriptionsProvider, RabbitMessageSubscriptionTopology>());

        context.Services.TryAddSingleton<RabbitMessageConsumer>();
        context.Services.TryAddSingleton<IRabbitMessageConsumer>(sp =>
            sp.GetRequiredService<RabbitMessageConsumer>());
        context.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMessageConsumer>());
    }
}
