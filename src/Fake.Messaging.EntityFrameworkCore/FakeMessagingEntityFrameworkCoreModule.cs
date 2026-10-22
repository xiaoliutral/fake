using Fake.EntityFrameworkCore;
using Fake.Messaging.Admin;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.Messaging;

[DependsOn(
    typeof(FakeMessagingModule),
    typeof(FakeEntityFrameworkCoreModule))]
public class FakeMessagingEntityFrameworkCoreModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Replace(ServiceDescriptor.Transient<IMessageOutboxStore, MessageOutboxStore>());
        context.Services.Replace(ServiceDescriptor.Transient<IMessageOutbox>(sp =>
            sp.GetRequiredService<IMessageOutboxStore>()));
        context.Services.Replace(ServiceDescriptor.Transient<IMessageInboxStore, MessageInboxStore>());
        // 覆盖 FakeMessagingModule 中的 NullMessageInbox
        context.Services.Replace(ServiceDescriptor.Transient<IMessageInbox>(sp =>
            sp.GetRequiredService<IMessageInboxStore>()));
        context.Services.Replace(ServiceDescriptor.Transient<IMessagePublisher, ReliableMessagePublisher>());
        context.Services.Replace(
            ServiceDescriptor.Transient<IMessageOutboxAdminStore, MessageOutboxAdminStore>());
        context.Services.Replace(
            ServiceDescriptor.Transient<IMessageInboxAdminStore, MessageInboxAdminStore>());
        context.Services.TryAddSingleton<OutboxDispatcher>();
        context.Services.Replace(ServiceDescriptor.Singleton<IMessageOutboxDispatcher>(sp =>
            sp.GetRequiredService<OutboxDispatcher>()));

        context.Services.AddHostedService<OutboxDispatcherBackgroundService>();
        context.Services.AddHostedService<MessagingInboxCleanupBackgroundService>();
    }
}
