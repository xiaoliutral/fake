using Fake.EventBus.Distributed;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

/// <summary>
/// EF Core 事务上下文实现
/// </summary>
public class EfCoreTransactionContext : ITransactionContext
{
    private readonly IDbContextTransaction _transaction;

    public EfCoreTransactionContext(IDbContextTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public Guid TransactionId => _transaction.TransactionId;

    public object GetUnderlyingTransaction() => _transaction;
}
