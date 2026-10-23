namespace Fake.Messaging;

/// <summary>
/// 消费侧工作单元策略（对齐 EventBus：默认 ambient 非事务）。
/// </summary>
public enum MessageConsumeUnitOfWorkMode
{
    /// <summary>未在 Attribute 指定，回填传输全局默认。</summary>
    Unspecified = 0,

    /// <summary>不开启 UoW。</summary>
    None = 1,

    /// <summary>ambient 非事务 UoW（可查库；写库请 RequiresNew）。</summary>
    NonTransactional = 2,

    /// <summary>ambient 事务 UoW。</summary>
    Transactional = 3
}
