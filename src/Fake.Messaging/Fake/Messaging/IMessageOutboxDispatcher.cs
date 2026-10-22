namespace Fake.Messaging;

/// <summary>
/// Outbox 投递调度（后台扫描 / 事务完成 / 人工重试）。
/// </summary>
public interface IMessageOutboxDispatcher
{
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);

    Task DispatchByTransactionAsync(string transactionId, CancellationToken cancellationToken = default);

    Task<bool> DispatchOneAsync(Guid messageId, CancellationToken cancellationToken = default);
}
