using System.Text.Json;
using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.Messaging.Admin;
using Fake.Messaging.EntityFrameworkCore;
using Fake.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.Messaging;

/// <summary>
/// 本机重新执行 Inbox 事件的 Handler（绕过消息队列，适合人工重试）。
/// </summary>
public class InboxEventReprocessor(
    MessagingDbContext context,
    IServiceScopeFactory scopeFactory,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<InboxEventReprocessor> logger) : IMessagingInboxLocalReprocessor
{
    public async Task<MessageInboxReprocessResult> ReprocessAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        // 与队列名（ConsumerGroup）解耦：按 MessageId + Kind 定位（同事件多消费者时取最近一条）
        var entry = await context.InboxMessages
            .Where(e => e.MessageId == messageId && e.Kind == MessageKind.EventBus)
            .OrderByDescending(e => e.ProcessedTime)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Inbox event {messageId} was not found.");

        if (string.IsNullOrWhiteSpace(entry.TypeName))
            return new MessageInboxReprocessResult(false, "Inbox entry missing TypeName.", entry.State);

        Type eventType;
        try
        {
            eventType = IntegrationEventTypeResolver.Resolve(entry.TypeName);
        }
        catch (Exception ex)
        {
            return new MessageInboxReprocessResult(false, ex.Message, entry.State);
        }

        IntegrationEvent integrationEvent;
        try
        {
            var deserialized = JsonSerializer.Deserialize(entry.Body, eventType) as IntegrationEvent;
            integrationEvent = deserialized
                               ?? throw new InvalidOperationException(
                                   $"Failed to deserialize inbox content as {eventType.FullName}.");
        }
        catch (Exception ex)
        {
            entry.MarkAsFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);
            return new MessageInboxReprocessResult(false, ex.Message, MessageInboxState.ConsumeFailed);
        }

        entry.MarkAsConsuming();
        await context.SaveChangesAsync(cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetKeyedServices<IEventHandler>(eventType).ToList();
        if (handlers.Count == 0)
        {
            var genericHandlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
            handlers = scope.ServiceProvider.GetServices(genericHandlerType)
                .OfType<IEventHandler>()
                .ToList();
        }

        if (handlers.Count == 0)
        {
            var msg = $"No IEventHandler registered for {eventType.FullName}.";
            entry.MarkAsNoHandler(msg);
            await context.SaveChangesAsync(cancellationToken);
            return new MessageInboxReprocessResult(false, msg, MessageInboxState.NoHandler);
        }

        try
        {
            using (var uow = unitOfWorkManager.Begin(new UnitOfWorkAttribute { IsTransactional = false }))
            {
                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(integrationEvent, cancellationToken);
                }

                await uow.CompleteAsync(cancellationToken);
            }

            entry.MarkAsSucceeded();
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Inbox event {EventId} reprocessed locally by {HandlerCount} handler(s)",
                messageId, handlers.Count);
            return new MessageInboxReprocessResult(true, State: MessageInboxState.ConsumeSucceeded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Inbox local reprocess failed for event {EventId}", messageId);
            entry.MarkAsFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);
            return new MessageInboxReprocessResult(false, ex.Message, MessageInboxState.ConsumeFailed);
        }
    }
}
