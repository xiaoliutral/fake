namespace Fake.EventBus.Distributed;

/// <summary>
/// Outbox 征用事务所用上下文（ORM 无关）。
/// 常见底层类型：<see cref="System.Data.Common.DbTransaction"/>；
/// 环境事务场景可为 <see cref="System.Transactions.Transaction"/> / TransactionScope。
/// </summary>
public interface ITransactionContext
{
    /// <summary>
    /// 事务 ID（用于关联 Outbox 事件）
    /// </summary>
    Guid TransactionId { get; }

    /// <summary>
    /// 获取底层事务对象（由存储实现解释，如 EF UseTransaction）
    /// </summary>
    object GetUnderlyingTransaction();
}
