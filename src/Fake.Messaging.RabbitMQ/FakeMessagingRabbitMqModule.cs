using Fake.Messaging.EntityFrameworkCore;
using Fake.Messaging.RabbitMQ;
using Fake.Modularity;
using Fake.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.Messaging;

[DependsOn(
    typeof(FakeMessagingEntityFrameworkCoreModule),
    typeof(FakeRabbitMqModule))]
public class FakeMessagingRabbitMqModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IPhysicalMessageTransport, RabbitPhysicalMessageTransport>());

        context.Services.TryAddSingleton<RabbitMessageConsumer>();
        context.Services.TryAddSingleton<IRabbitMessageConsumer>(sp =>
            sp.GetRequiredService<RabbitMessageConsumer>());
        context.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMessageConsumer>());
    }
}
