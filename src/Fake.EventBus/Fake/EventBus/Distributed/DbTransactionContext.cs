using System.Data.Common;

namespace Fake.EventBus.Distributed;

/// <summary>
/// 基于 ADO.NET <see cref="DbTransaction"/> 的事务上下文（ORM 无关）。
/// </summary>
public sealed class DbTransactionContext : ITransactionContext
{
    private readonly DbTransaction _dbTransaction;

    public DbTransactionContext(Guid transactionId, DbTransaction dbTransaction)
    {
        TransactionId = transactionId;
        _dbTransaction = dbTransaction ?? throw new ArgumentNullException(nameof(dbTransaction));
    }

    public Guid TransactionId { get; }

    public object GetUnderlyingTransaction() => _dbTransaction;
}
