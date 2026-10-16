using System.Data.Common;

namespace Fake.UnitOfWork;

/// <summary>
/// 同一工作单元、同一连接串下跨 ORM（EF / SqlSugar 等）共享物理 <see cref="DbTransaction"/>。
/// 先开事务的一方登记为 owner；后到的 ORM 只 enlist，不再 Begin / Commit。
/// </summary>
public static class UnitOfWorkSharedDb
{
    private const string ItemKeyPrefix = "Fake.UnitOfWork.SharedDbTransaction:";

    public static string NormalizeConnectionString(string? connectionString)
        => (connectionString ?? string.Empty).Trim();

    public static string ItemKey(string connectionString)
        => ItemKeyPrefix + NormalizeConnectionString(connectionString);

    /// <summary>
    /// 查找可征用的共享事务（优先 Items 登记，其次扫描已激活的 <see cref="IOutboxEnlistableTransaction"/>）。
    /// </summary>
    public static IOutboxEnlistableTransaction? Find(IUnitOfWork unitOfWork, string connectionString)
    {
        ThrowHelper.ThrowIfNull(unitOfWork, nameof(unitOfWork));

        var key = ItemKey(connectionString);
        if (unitOfWork.Items.TryGetValue(key, out var boxed)
            && boxed is IOutboxEnlistableTransaction fromItem
            && fromItem.GetDbTransaction() != null)
        {
            return fromItem;
        }

        var normalized = NormalizeConnectionString(connectionString);
        if (normalized.Length == 0)
        {
            return null;
        }

        foreach (var api in unitOfWork.GetAllActiveTransactionApis().OfType<IOutboxEnlistableTransaction>())
        {
            var tx = api.GetDbTransaction();
            var cs = tx?.Connection?.ConnectionString;
            if (cs == null)
            {
                continue;
            }

            if (!string.Equals(NormalizeConnectionString(cs), normalized, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            unitOfWork.Items[key] = api;
            return api;
        }

        return null;
    }

    public static DbTransaction? GetDbTransaction(IUnitOfWork unitOfWork, string connectionString)
        => Find(unitOfWork, connectionString)?.GetDbTransaction();

    /// <summary>
    /// 返回本 UoW 中任意一条已登记的共享事务（供 Outbox 优先征用 owner）。
    /// </summary>
    public static IOutboxEnlistableTransaction? FindAny(IUnitOfWork unitOfWork)
    {
        ThrowHelper.ThrowIfNull(unitOfWork, nameof(unitOfWork));

        foreach (var (key, value) in unitOfWork.Items)
        {
            if (!key.StartsWith(ItemKeyPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (value is IOutboxEnlistableTransaction tx && tx.GetDbTransaction() != null)
            {
                return tx;
            }
        }

        return unitOfWork.GetAllActiveTransactionApis()
            .OfType<IOutboxEnlistableTransaction>()
            .FirstOrDefault(t => t.GetDbTransaction() != null);
    }

    /// <summary>
    /// 将当前 ORM 开启的事务登记为该连接串的共享 owner（仅应在真正 Begin 后调用一次）。
    /// </summary>
    public static void Register(
        IUnitOfWork unitOfWork,
        string connectionString,
        IOutboxEnlistableTransaction transaction)
    {
        ThrowHelper.ThrowIfNull(unitOfWork, nameof(unitOfWork));
        ThrowHelper.ThrowIfNull(transaction, nameof(transaction));

        unitOfWork.Items[ItemKey(connectionString)] = transaction;
    }
}
