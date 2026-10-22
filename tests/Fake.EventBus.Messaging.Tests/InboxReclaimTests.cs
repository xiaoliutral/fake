using Fake.EventBus.Messaging.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class InboxReclaimTests : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task Consuming锁未过期_非强制抢占应失败()
    {
        var eventId = Guid.NewGuid();
        var type = typeof(TestOutboxIntegrationEvent).FullName!;

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, eventId, type, "{}"))
                .ShouldBeTrue();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(
                scope.ServiceProvider, eventId, type, "{}", forceReclaim: false))
                .ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Consuming锁未过期_Redelivered强制抢占应成功()
    {
        var eventId = Guid.NewGuid();
        var type = typeof(TestOutboxIntegrationEvent).FullName!;

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, eventId, type, "{}"))
                .ShouldBeTrue();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(
                scope.ServiceProvider, eventId, type, "{}", forceReclaim: true))
                .ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ConsumeSucceeded_即使强制抢占也应跳过()
    {
        var eventId = Guid.NewGuid();
        var type = typeof(TestOutboxIntegrationEvent).FullName!;

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, eventId, type, "{}"))
                .ShouldBeTrue();
            await TestMessagingHelper.MarkInboxSucceededAsync(scope.ServiceProvider, eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(
                scope.ServiceProvider, eventId, type, "{}", forceReclaim: true))
                .ShouldBeFalse();
        }
    }
}
