using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.EntityFrameworkCore;

/// <summary>
/// 按 Destination 分流到 <see cref="IPhysicalMessageTransport"/>。
/// </summary>
public class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<OutboxDispatcher> logger) : IMessageOutboxDispatcher
{
    private readonly MessagingOptions _options = options.Value;

    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MessageOutboxRecord> pending;
        await using (var scanScope = scopeFactory.CreateAsyncScope())
        {
            var store = scanScope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
            pending = await store.GetPendingAsync(_options.OutboxBatchSize, cancellationToken);
        }

        if (pending.Count == 0)
            return;

        logger.LogDebug("Outbox dispatcher found {Count} pending messages", pending.Count);

        await Parallel.ForEachAsync(
            pending,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.OutboxMaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (record, ct) => await DispatchOneAsync(record, source: "scan", throwOnFailure: false, ct));
    }

    public async Task DispatchByTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var pending = await store.GetPendingByTransactionAsync(transactionId, cancellationToken);

        foreach (var record in pending)
        {
            await DispatchOneAsync(record, source: "immediate", throwOnFailure: false, cancellationToken);
        }
    }

    public async Task<bool> DispatchOneAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var record = await store.FindAsync(messageId, cancellationToken)
                     ?? throw new InvalidOperationException($"Outbox message {messageId} was not found.");

        return await DispatchOneAsync(record, source: "manual", throwOnFailure: false, cancellationToken);
    }

    private async Task<bool> DispatchOneAsync(
        MessageOutboxRecord record,
        string source,
        bool throwOnFailure,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var transports = scope.ServiceProvider.GetServices<IPhysicalMessageTransport>().ToList();

        try
        {
            var lockTimeout = _options.EnableZombieLockRecovery ? _options.OutboxLockTimeout : (TimeSpan?)null;
            var locked = await store.TryMarkInProgressAsync(record.MessageId, lockTimeout, cancellationToken);
            if (!locked)
            {
                logger.LogDebug("Outbox message {MessageId} skipped (lock not acquired)", record.MessageId);
                return false;
            }

            var transport = transports.FirstOrDefault(t => t.CanPublish(record.Destination))
                            ?? throw new InvalidOperationException(
                                $"No IPhysicalMessageTransport for destination '{record.Destination}'.");

            await transport.PublishAsync(record.ToEnvelope(), cancellationToken);
            await store.MarkPublishedAsync(record.MessageId, cancellationToken);

            logger.LogInformation(
                "Outbox message {MessageId} dispatched (source={Source}, destination={Destination})",
                record.MessageId, source, record.Destination);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Outbox dispatch failed for {MessageId} (source={Source})",
                record.MessageId, source);

            try
            {
                await store.MarkPublishFailedAsync(record.MessageId, ex.Message, cancellationToken);
            }
            catch (Exception markEx)
            {
                logger.LogError(markEx, "Failed to mark outbox {MessageId} as PublishFailed", record.MessageId);
            }

            if (throwOnFailure)
                throw;

            return false;
        }
    }
}
