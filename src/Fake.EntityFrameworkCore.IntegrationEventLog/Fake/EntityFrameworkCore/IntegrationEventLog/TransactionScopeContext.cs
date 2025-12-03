using System.Transactions;
using Fake.EventBus.Distributed;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

/// <summary>
/// TransactionScope 事务上下文实现
/// </summary>
public class TransactionScopeContext : ITransactionContext
{
    private readonly TransactionScope _transactionScope;

    public TransactionScopeContext(TransactionScope transactionScope)
    {
        _transactionScope = transactionScope ?? throw new ArgumentNullException(nameof(transactionScope));
    }

    /// <summary>
    /// TransactionScope 没有显式的 TransactionId，使用当前环境事务的 ID
    /// </summary>
    public Guid TransactionId
    {
        get
        {
            var current = Transaction.Current;
            if (current == null)
            {
                throw new InvalidOperationException("No ambient transaction found. Ensure TransactionScope is active.");
            }

            // 使用环境事务的 TransactionInformation.LocalIdentifier 作为事务 ID
            // 注意：这是一个字符串，我们需要生成一个稳定的 Guid
            var localId = current.TransactionInformation.LocalIdentifier;
            return GenerateGuidFromString(localId);
        }
    }

    public object GetUnderlyingTransaction() => _transactionScope;

    /// <summary>
    /// 从字符串生成稳定的 Guid（使用 MD5 哈希）
    /// </summary>
    private static Guid GenerateGuidFromString(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
