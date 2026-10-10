using System.Data;
using Fake.UnitOfWork;

namespace Fake.EventBus.IntegrationEventLog.Tests;

internal static class OutboxTestHelper
{
    /// <summary>
    /// Sqlite 不支持 IsolationLevel.Unspecified(0)；UnitOfWorkAttribute 的 IsolationLevel 非可空，必须显式指定。
    /// </summary>
    public static UnitOfWorkAttribute TransactionalUow() => new()
    {
        IsTransactional = true,
        IsolationLevel = IsolationLevel.ReadCommitted
    };
}
