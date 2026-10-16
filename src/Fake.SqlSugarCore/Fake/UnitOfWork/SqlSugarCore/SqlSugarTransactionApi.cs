using System.Data.Common;
using Fake.SqlSugarCore;

namespace Fake.UnitOfWork.SqlSugarCore;

/// <summary>
/// SqlSugarCore 事务 api。
/// <paramref name="ownsTransaction"/> 为 false 时表示已 enlist 到共享 <see cref="DbTransaction"/>，Commit/Rollback/Dispose 由 owner 负责。
/// </summary>
public class SqlSugarTransactionApi<TDbContext> : ITransactionApi, ISupportRollback, IOutboxEnlistableTransaction
    where TDbContext : SugarDbContext<TDbContext>
{
    private readonly SugarDbContext<TDbContext> _sugarDbContext;
    private readonly bool _ownsTransaction;

    public Guid TransactionId { get; } = Guid.NewGuid();

    public bool OwnsTransaction => _ownsTransaction;

    public SqlSugarTransactionApi(SugarDbContext<TDbContext> sugarDbContext, bool ownsTransaction = true)
    {
        _sugarDbContext = sugarDbContext;
        _ownsTransaction = ownsTransaction;
    }

    public DbTransaction? GetDbTransaction()
        => _sugarDbContext.SqlSugarClient.Ado.Transaction as DbTransaction;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!_ownsTransaction)
        {
            return;
        }

        await _sugarDbContext.SqlSugarClient.Ado.CommitTranAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (!_ownsTransaction)
        {
            return;
        }

        await _sugarDbContext.SqlSugarClient.Ado.RollbackTranAsync();
    }

    public SugarDbContext<TDbContext> GetDbContext()
    {
        return _sugarDbContext;
    }

    public void Dispose()
    {
        if (!_ownsTransaction)
        {
            return;
        }

        _sugarDbContext.SqlSugarClient.Ado.Dispose();
    }
}
