using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Messaging.EntityFrameworkCore;

/// <summary>
/// 独立队列可靠发布：有事务则写 Outbox，否则立即经物理传输投递。
/// </summary>
public class ReliableMessagePublisher(
    IUnitOfWorkManager unitOfWorkManager,
    IServiceProvider serviceProvider,
    OutboxDispatcher dispatcher) : IMessagePublisher
{
    private const string PendingKey = "Fake.Messaging.Outbox.Pending";
    private const string TxIdsKey = "Fake.Messaging.Outbox.TransactionIds";
    private const string HandlersKey = "Fake.Messaging.Outbox.Handlers";

    public async Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var uow = unitOfWorkManager.Current;
        if (uow == null || !uow.Context.IsTransactional)
        {
            var transports = serviceProvider.GetServices<IPhysicalMessageTransport>().ToList();
            var transport = transports.FirstOrDefault(t => t.CanPublish(envelope.Destination))
                            ?? throw new InvalidOperationException(
                                $"No IPhysicalMessageTransport for destination '{envelope.Destination}'.");
            await transport.PublishAsync(envelope, cancellationToken);
            return;
        }

        var enlistable = FindEnlistable(uow);
        if (enlistable?.GetDbTransaction() != null)
        {
            await SaveAsync(uow, envelope, enlistable, cancellationToken);
        }
        else
        {
            var pending = uow.GetOrAddItem(PendingKey, _ => new List<MessageEnvelope>());
            pending.Add(envelope);
        }

        EnsureHandlers(uow);
    }

    private void EnsureHandlers(IUnitOfWork uow)
    {
        if (!uow.Items.TryAdd(HandlersKey, true))
            return;

        uow.OnSaveChanged(async () =>
        {
            var pending = uow.GetOrAddItem(PendingKey, _ => new List<MessageEnvelope>());
            if (pending.Count == 0)
                return;

            var enlistable = FindEnlistable(uow)
                             ?? throw new InvalidOperationException(
                                 "Pending messaging envelopes but no enlistable DB transaction.");

            foreach (var env in pending.ToList())
                await SaveAsync(uow, env, enlistable, CancellationToken.None);

            pending.Clear();
        });

        uow.OnCompleted(async () =>
        {
            var txIds = uow.GetOrAddItem(TxIdsKey, _ => new HashSet<string>());
            foreach (var txId in txIds)
                await dispatcher.DispatchByTransactionAsync(txId);
        });
    }

    private async Task SaveAsync(
        IUnitOfWork uow,
        MessageEnvelope envelope,
        IOutboxEnlistableTransaction enlistable,
        CancellationToken cancellationToken)
    {
        var dbTx = enlistable.GetDbTransaction()
                   ?? throw new InvalidOperationException("GetDbTransaction() returned null.");

        var store = uow.ServiceProvider.GetRequiredService<IMessageOutboxStore>();
        var withTx = envelope.TransactionId == null
            ? new MessageEnvelope
            {
                MessageId = envelope.MessageId,
                Destination = envelope.Destination,
                Kind = envelope.Kind,
                Body = envelope.Body,
                Headers = envelope.Headers,
                TypeName = envelope.TypeName,
                TransactionId = enlistable.TransactionId.ToString()
            }
            : envelope;

        await store.EnqueueInTransactionAsync(withTx, dbTx, cancellationToken);

        var txIds = uow.GetOrAddItem(TxIdsKey, _ => new HashSet<string>());
        txIds.Add(enlistable.TransactionId.ToString());
    }

    private static IOutboxEnlistableTransaction? FindEnlistable(IUnitOfWork uow)
        => UnitOfWorkSharedDb.FindAny(uow)
           ?? uow.GetAllActiveTransactionApis().OfType<IOutboxEnlistableTransaction>().FirstOrDefault();
}
