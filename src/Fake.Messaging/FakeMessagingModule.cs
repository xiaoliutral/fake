using Fake.DependencyInjection;
using Fake.Messaging.Admin;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.Messaging;

public class FakeMessagingModule : FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnServiceExposing(exposingContext =>
        {
            var impl = exposingContext.ImplementationType;
            if (impl.IsAbstract || impl.IsInterface)
                return;

            var isHandler = false;
            foreach (var iface in impl.GetInterfaces())
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IMessageHandle<>))
                    continue;

                exposingContext.ExposedServices.TryAdd(new ServiceIdentifier(iface));
                isHandler = true;
            }

            if (isHandler)
                GetOrCreateTypeStore(context.Services).Add(impl);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var store = GetOrCreateTypeStore(context.Services);
        context.Services.TryAddSingleton(store);
        context.Services.TryAddSingleton<MessageSubscribeFinder>();
        context.Services.AddHostedService<MessageSubscribeHostedService>();

        var configuration = context.Services.GetConfiguration();
        context.Services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Validate(o =>
            {
                o.Validate();
                return true;
            }, "Invalid MessagingOptions")
            .ValidateOnStart();

        context.Services.TryAddSingleton<IMessageRouteResolver, DefaultMessageRouteResolver>();
        context.Services.TryAddSingleton<IMessageBodySerializer>(_ => SystemTextJsonMessageBodySerializer.Instance);
        // 无 Outbox 直投；FakeMessagingEntityFrameworkCoreModule 会 Replace 为 ReliableMessagePublisher
        context.Services.TryAddTransient<IMessagePublisher, DirectMessagePublisher>();

        context.Services.TryAddTransient<IMessageInbox>(_ => NullMessageInbox.Instance);
        context.Services.TryAddTransient<IMessageOutboxAdminStore>(_ => NullMessageOutboxAdminStore.Instance);
        context.Services.TryAddTransient<IMessageInboxAdminStore>(_ => NullMessageInboxAdminStore.Instance);
        context.Services.TryAddSingleton<IMessageOutboxDispatcher>(_ => NullMessageOutboxDispatcher.Instance);
    }

    private static MessageSubscribeTypeStore GetOrCreateTypeStore(IServiceCollection services)
    {
        var accessor = services.GetOrAddObjectAccessor<MessageSubscribeTypeStore>();
        if (accessor.Value != null)
            return accessor.Value;

        var store = new MessageSubscribeTypeStore();
        accessor.Value = store;
        return store;
    }
}
