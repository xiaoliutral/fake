using Fake.EventBus.Distributed;
using Fake.EventBus.Messaging.Tests.Events;
using Fake.Messaging;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class ApplicationEventHandlerTests
    : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task 无事务时Outbox扩展Enqueue应失败()
    {
        var store = ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var resolver = ServiceProvider.GetRequiredService<IEventRoutingKeyResolver>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await store.EnqueueIntegrationEventAsync(new TestOutboxIntegrationEvent(), resolver));
    }
}
