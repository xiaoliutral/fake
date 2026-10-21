using System.Text.Json;
using Fake.EventBus.Distributed;
using Fake.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// 本机重新执行 Inbox 事件的 Handler（绕过消息队列，适合人工重试）。
/// 与 RabbitMqEventBus 消费路径一致：Handle 前开非事务 UoW。
/// </summary>
public class InboxEventReprocessor(
    IntegrationEventLogContext context,
    IServiceScopeFactory scopeFactory,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<InboxEventReprocessor> logger) : IInboxEventReprocessor
{
    public async Task<InboxReprocessResult> ReprocessAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var entry = await context.InboxEventLogs
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken)
            ?? throw new InvalidOperationException($"Inbox event {eventId} was not found.");

        Type eventType;
        try
        {
            eventType = IntegrationEventTypeResolver.Resolve(entry.EventTypeName);
        }
        catch (Exception ex)
        {
            return new InboxReprocessResult(false, ex.Message, entry.State);
        }

        IntegrationEvent integrationEvent;
        try
        {
            var deserialized = JsonSerializer.Deserialize(entry.Content, eventType) as IntegrationEvent;
            integrationEvent = deserialized
                               ?? throw new InvalidOperationException(
                                   $"Failed to deserialize inbox content as {eventType.FullName}.");
        }
        catch (Exception ex)
        {
            entry.MarkAsFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);
            return new InboxReprocessResult(false, ex.Message, EventState.ConsumeFailed);
        }

        entry.MarkAsConsuming();
        await context.SaveChangesAsync(cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetKeyedServices<IEventHandler>(eventType).ToList();
        if (handlers.Count == 0)
        {
            // 兼容：部分宿主可能只注册了 IEventHandler<T>
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
            return new InboxReprocessResult(false, msg, EventState.NoHandler);
        }

        try
        {
            // 与 RabbitMqEventBus.ProcessingEventAsync 对齐：默认非事务 UoW，便于查库
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
                eventId, handlers.Count);
            return new InboxReprocessResult(true, State: EventState.ConsumeSucceeded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Inbox local reprocess failed for event {EventId}", eventId);
            entry.MarkAsFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);
            return new InboxReprocessResult(false, ex.Message, EventState.ConsumeFailed);
        }
    }
}
