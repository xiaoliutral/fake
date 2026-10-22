using Fake.EventBus.Distributed;
using Fake.EventBus.Messaging.Tests.Events;
using Fake.EventBus.Messaging.Tests.Fakes;
using Fake.Messaging;
using Fake.Messaging.Admin;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class OutboxAdminRetryTests : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
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
            var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            var evt = new TestOutboxIntegrationEvent { Payload = "retry-reset" };
            eventId = evt.Id;
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, evt, ctx);
            await tx.CommitAsync();
            await store.MarkPublishFailedAsync(eventId, "fail");
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageOutboxAdminStore>();
            await admin.ResetForRetryAsync(eventId);

            var item = await admin.FindAsync(eventId);
            item.ShouldNotBeNull();
            item!.State.ShouldBe(MessageOutboxState.NotPublished);
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
            var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            var evt = new TestOutboxIntegrationEvent { Payload = "retry-publish-now" };
            eventId = evt.Id;
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, evt, ctx);
            await tx.CommitAsync();
            await store.MarkPublishFailedAsync(eventId, "fail");
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageOutboxAdminStore>();
            var publisher = scope.ServiceProvider.GetRequiredService<IOutboxEventPublisher>();

            await admin.ResetForRetryAsync(eventId);
            await publisher.PublishOneAsync(eventId);
        }

        physical.Published.Count.ShouldBe(1);
        physical.Published[0].Id.ShouldBe(eventId);

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageOutboxAdminStore>();
            var item = await admin.FindAsync(eventId);
            item!.State.ShouldBe(MessageOutboxState.Published);
        }
    }

    [Fact]
    public async Task Inbox人工重试应重置为可抢占()
    {
        Guid eventId = Guid.NewGuid();
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, eventId, "Test.Event", "{}"))
                .ShouldBeTrue();
            await TestMessagingHelper.MarkInboxSucceededAsync(scope.ServiceProvider, eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageInboxAdminStore>();
            await admin.ResetForRetryAsync(eventId);
            var item = await admin.FindAsync(eventId);
            item!.State.ShouldBe(MessageInboxState.ConsumeFailed);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(scope.ServiceProvider, eventId, "Test.Event", "{}"))
                .ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Outbox重投前ClearInbox_已成功的Inbox应变可抢占()
    {
        Guid eventId;
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            await using var tx = await ctx.Database.BeginTransactionAsync();
            var evt = new TestOutboxIntegrationEvent { Payload = "clear-inbox" };
            eventId = evt.Id;
            await TestMessagingHelper.EnqueueAsync(scope.ServiceProvider, evt, ctx);
            await tx.CommitAsync();
            await store.MarkPublishedAsync(eventId);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(
                scope.ServiceProvider,
                eventId,
                typeof(TestOutboxIntegrationEvent).FullName!,
                """{"Payload":"clear-inbox"}""")).ShouldBeTrue();
            await TestMessagingHelper.MarkInboxSucceededAsync(scope.ServiceProvider, eventId);
            (await TestMessagingHelper.IsInboxSucceededAsync(scope.ServiceProvider, eventId)).ShouldBeTrue();
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var outboxAdmin = scope.ServiceProvider.GetRequiredService<IMessageOutboxAdminStore>();
            var inboxAdmin = scope.ServiceProvider.GetRequiredService<IMessageInboxAdminStore>();
            var publisher = scope.ServiceProvider.GetRequiredService<IOutboxEventPublisher>();
            var physical = scope.ServiceProvider.GetRequiredService<FakePhysicalDistributedEventBus>();
            physical.Clear();

            await outboxAdmin.ResetForRetryAsync(eventId);
            (await inboxAdmin.TryResetForRetryAsync(eventId)).ShouldBeTrue();

            var result = await publisher.PublishOneAsync(eventId);
            result.Published.ShouldBeTrue();
            physical.Published.Count.ShouldBe(1);
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.IsInboxSucceededAsync(scope.ServiceProvider, eventId)).ShouldBeFalse();
            (await TestMessagingHelper.TryBeginInboxAsync(
                scope.ServiceProvider,
                eventId,
                typeof(TestOutboxIntegrationEvent).FullName!,
                """{"Payload":"clear-inbox"}""")).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Inbox本地Reprocess应执行Handler()
    {
        TrackingInboxHandler.Reset();
        var evt = new TestOutboxIntegrationEvent { Payload = "reprocess-me" };
        var json = System.Text.Json.JsonSerializer.Serialize(evt);

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            (await TestMessagingHelper.TryBeginInboxAsync(
                scope.ServiceProvider, evt.Id, typeof(TestOutboxIntegrationEvent).FullName!, json))
                .ShouldBeTrue();
            await TestMessagingHelper.MarkInboxFailedAsync(scope.ServiceProvider, evt.Id, "boom");
        }

        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IMessageInboxAdminStore>();
            var reprocessor = scope.ServiceProvider.GetRequiredService<IMessagingInboxLocalReprocessor>();
            await admin.ResetForRetryAsync(evt.Id);
            var result = await reprocessor.ReprocessAsync(evt.Id);
            result.Processed.ShouldBeTrue(result.Error);
            result.State.ShouldBe(MessageInboxState.ConsumeSucceeded);
        }

        TrackingInboxHandler.Handled.ShouldContain(evt.Id);
    }
}
