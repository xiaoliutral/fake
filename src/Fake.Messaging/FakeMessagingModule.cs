using Fake.Messaging.Admin;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.Messaging;

public class FakeMessagingModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Validate(o =>
            {
                o.Validate();
                return true;
            }, "Invalid MessagingOptions")
            .ValidateOnStart();

        context.Services.TryAddTransient<IMessageInbox>(_ => NullMessageInbox.Instance);
        context.Services.TryAddTransient<IMessageOutboxAdminStore>(_ => NullMessageOutboxAdminStore.Instance);
        context.Services.TryAddTransient<IMessageInboxAdminStore>(_ => NullMessageInboxAdminStore.Instance);
        context.Services.TryAddSingleton<IMessageOutboxDispatcher>(_ => NullMessageOutboxDispatcher.Instance);
    }
}
