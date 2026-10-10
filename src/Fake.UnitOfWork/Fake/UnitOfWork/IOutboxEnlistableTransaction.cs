using System.Data.Common;

namespace Fake.UnitOfWork;

/// <summary>
/// 可被 Outbox 征用的工作单元事务（EF / SqlSugar 等实现）。
/// 总线与存储只依赖本抽象，不直接依赖具体 ORM TransactionApi。
/// </summary>
public interface IOutboxEnlistableTransaction : ITransactionApi
{
    /// <summary>
    /// 事务标识（写入 Outbox.TransactionId，供 Commit 后按事务拉取）
    /// </summary>
    Guid TransactionId { get; }

    /// <summary>
    /// 底层 ADO.NET 事务；Outbox 存储（如 EF）通过它加入同一物理事务。
    /// 若 ORM 无法暴露则为 null（此时无法与基于连接的 Outbox 存储共享事务）。
    /// </summary>
    DbTransaction? GetDbTransaction();
}
