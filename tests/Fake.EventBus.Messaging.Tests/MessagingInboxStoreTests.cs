using Fake.Messaging;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Messaging.Tests;

public class MessagingInboxStoreTests : ApplicationTestWithTools<FakeEventBusMessagingTestModule>
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    [Fact]
    public async Task TryBegin_同ConsumerGroup幂等_成功后不可再抢占()
    {
        var messageId = Guid.NewGuid();
        var envelope = new MessageEnvelope
        {
            MessageId = messageId,
            Destination = MessageDestinations.ForEventBus("test"),
            Kind = MessageKind.EventBus,
            Body = "{}",
            TypeName = "TestEvent"
        };

        await using var scope = ServiceProvider.CreateAsyncScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IMessageInbox>();

        (await inbox.TryBeginAsync(messageId, "g1", envelope)).ShouldBeTrue();
        await inbox.MarkSucceededAsync(messageId, "g1");

        (await inbox.TryBeginAsync(messageId, "g1", envelope, forceReclaim: true)).ShouldBeFalse();
        (await inbox.TryBeginAsync(messageId, "g2", envelope)).ShouldBeTrue();
    }

    [Fact]
    public async Task TryBegin_ConsumeFailed后可抢占()
    {
        var messageId = Guid.NewGuid();
        var envelope = new MessageEnvelope
        {
            MessageId = messageId,
            Destination = MessageDestinations.ForRabbit("ex", "rk"),
            Kind = MessageKind.Rabbit,
            Body = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{}")),
            TypeName = "OrderExport"
        };

        await using var scope = ServiceProvider.CreateAsyncScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IMessageInbox>();

        (await inbox.TryBeginAsync(messageId, "order.q", envelope)).ShouldBeTrue();
        await inbox.MarkFailedAsync(messageId, "order.q", "boom");
        (await inbox.TryBeginAsync(messageId, "order.q", envelope)).ShouldBeTrue();
    }

    [Fact]
    public async Task Outbox_Enqueue后可按Destination分流查询()
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var messageId = Guid.NewGuid();

        await store.EnqueueAsync(new MessageEnvelope
        {
            MessageId = messageId,
            Destination = MessageDestinations.ForRabbit("ex.orders", "order.export"),
            Kind = MessageKind.Rabbit,
            Body = Convert.ToBase64String([1, 2, 3]),
            TransactionId = Guid.NewGuid().ToString(),
            TypeName = "bytes"
        });

        var found = await store.FindAsync(messageId);
        found.ShouldNotBeNull();
        found!.Kind.ShouldBe(MessageKind.Rabbit);
        found.Destination.ShouldBe("rabbit:ex.orders/order.export");
        found.State.ShouldBe(MessageOutboxState.NotPublished);
    }
}
