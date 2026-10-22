using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class InboxReclaimTests : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task Consuming锁未过期_非强制抢占应失败()
    {
        var eventId = Guid.NewGuid();
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(eventId, typeof(TestOutboxIntegrationEvent).FullName!, "{}"))
                .ShouldBeTrue();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(
                eventId, typeof(TestOutboxIntegrationEvent).FullName!, "{}", forceReclaim: false))
                .ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Consuming锁未过期_Redelivered强制抢占应成功()
    {
        var eventId = Guid.NewGuid();
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(eventId, typeof(TestOutboxIntegrationEvent).FullName!, "{}"))
                .ShouldBeTrue();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(
                eventId, typeof(TestOutboxIntegrationEvent).FullName!, "{}", forceReclaim: true))
                .ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ConsumeSucceeded_即使强制抢占也应跳过()
    {
        var eventId = Guid.NewGuid();
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(eventId, typeof(TestOutboxIntegrationEvent).FullName!, "{}"))
                .ShouldBeTrue();
            await inbox.MarkAsSucceededAsync(eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(
                eventId, typeof(TestOutboxIntegrationEvent).FullName!, "{}", forceReclaim: true))
                .ShouldBeFalse();
        }
    }
}
