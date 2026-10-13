using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.EventBus.IntegrationEventLog.Tests.Fakes;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.IntegrationEventLog.Tests;

public class OutboxAdminRetryTests : ApplicationTestWithTools<FakeEventBusIntegrationEventLogTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task 人工重试_仅改状态等待扫描()
    {
        var physical = ServiceProvider.GetRequiredService<FakePhysicalDistributedEventBus>();
        physical.Clear();

        Guid eventId;
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            var evt = new TestOutboxIntegrationEvent { Payload = "retry-reset" };
            eventId = evt.Id;
            await outbox.SaveEventAsync(evt, ctx);
            await tx.CommitAsync();
            await outbox.MarkEventAsFailedAsync(eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IOutboxAdminStore>();
            await admin.ResetForRetryAsync(eventId);

            var item = await admin.FindAsync(eventId);
            item.ShouldNotBeNull();
            item!.State.ShouldBe(EventState.NotPublished);
        }

        physical.Published.Count.ShouldBe(0);
    }

    [Fact]
    public async Task 人工重试_改状态并立即Publish()
    {
        var physical = ServiceProvider.GetRequiredService<FakePhysicalDistributedEventBus>();
        physical.Clear();

        Guid eventId;
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            var evt = new TestOutboxIntegrationEvent { Payload = "retry-publish-now" };
            eventId = evt.Id;
            await outbox.SaveEventAsync(evt, ctx);
            await tx.CommitAsync();
            await outbox.MarkEventAsFailedAsync(eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IOutboxAdminStore>();
            var publisher = scope.ServiceProvider.GetRequiredService<IOutboxEventPublisher>();

            await admin.ResetForRetryAsync(eventId);
            await publisher.PublishOneAsync(eventId);
        }

        physical.Published.Count.ShouldBe(1);
        physical.Published[0].Id.ShouldBe(eventId);

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IOutboxAdminStore>();
            var item = await admin.FindAsync(eventId);
            item!.State.ShouldBe(EventState.Published);
        }
    }

    [Fact]
    public async Task Inbox人工重试应重置为可抢占()
    {
        Guid eventId = Guid.NewGuid();
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(eventId, "Test.Event", "{}")).ShouldBeTrue();
            await inbox.MarkAsSucceededAsync(eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IInboxAdminStore>();
            await admin.ResetForRetryAsync(eventId);
            var item = await admin.FindAsync(eventId);
            item!.State.ShouldBe(EventState.ConsumeFailed);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();
            (await inbox.TryMarkAsProcessingAsync(eventId, "Test.Event", "{}")).ShouldBeTrue();
        }
    }
}
