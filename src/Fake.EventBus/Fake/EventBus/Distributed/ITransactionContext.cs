namespace Fake.EventBus.Distributed;

/// <summary>
/// 事务上下文抽象接口（支持 EF Core、TransactionScope、Dapper 等）
/// </summary>
public interface ITransactionContext
{
    /// <summary>
    /// 事务 ID（用于关联 Outbox 事件）
    /// </summary>
    Guid TransactionId { get; }
    
    /// <summary>
    /// 获取底层事务对象（供具体实现使用）
    /// </summary>
    object GetUnderlyingTransaction();
}
