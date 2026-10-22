using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.Messaging.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.EventBus.Messaging.Tests;

internal static class TestMessagingHelper
{
    /// <summary>测试用 Inbox ConsumerGroup（与历史 DefaultConsumerGroup 默认值一致）。</summary>
    public const string ConsumerGroup = "eventbus";

    public static async Task EnqueueAsync(
        IServiceProvider sp,
        Event @event,
        MessagingDbContext ctx)
    {
        var store = sp.GetRequiredService<IMessageOutboxStore>();
        var resolver = sp.GetRequiredService<IEventRoutingKeyResolver>();
        await store.EnqueueIntegrationEventAsync(@event, ctx, resolver);
    }

    public static MessageEnvelope InboxEnvelope(Guid eventId, string typeName, string content)
        => EventBusMessageFactory.CreateInboxEnvelope(eventId, typeName, content);

    public static async Task<bool> TryBeginInboxAsync(
        IServiceProvider sp,
        Guid eventId,
        string typeName,
        string content,
        bool forceReclaim = false)
    {
        var inbox = sp.GetRequiredService<IMessageInbox>();
        return await inbox.TryBeginAsync(
            eventId, ConsumerGroup, InboxEnvelope(eventId, typeName, content), forceReclaim);
    }

    public static Task MarkInboxSucceededAsync(IServiceProvider sp, Guid eventId)
        => sp.GetRequiredService<IMessageInbox>().MarkSucceededAsync(eventId, ConsumerGroup);

    public static Task MarkInboxFailedAsync(IServiceProvider sp, Guid eventId, string error)
        => sp.GetRequiredService<IMessageInbox>().MarkFailedAsync(eventId, ConsumerGroup, error);

    public static async Task<bool> IsInboxSucceededAsync(IServiceProvider sp, Guid eventId)
    {
        var store = sp.GetRequiredService<IMessageInboxStore>();
        var record = await store.FindAsync(eventId, ConsumerGroup);
        return record?.State == MessageInboxState.ConsumeSucceeded;
    }

    public static async Task<IReadOnlyList<MessageOutboxRecord>> GetPendingEventBusAsync(
        IServiceProvider sp,
        int maxCount = 50)
    {
        var store = sp.GetRequiredService<IMessageOutboxStore>();
        var all = await store.GetPendingAsync(maxCount);
        return all.Where(r => r.Kind == MessageKind.EventBus).ToList();
    }
}
