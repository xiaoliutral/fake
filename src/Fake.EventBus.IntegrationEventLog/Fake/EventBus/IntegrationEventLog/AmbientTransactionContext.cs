using System.Transactions;
using Fake.EventBus.Distributed;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// 基于环境 Transaction（TransactionScope）的事务上下文，不创建嵌套 Scope。
/// </summary>
public class AmbientTransactionContext : ITransactionContext
{
    public Guid TransactionId
    {
        get
        {
            var current = Transaction.Current
                          ?? throw new InvalidOperationException(
                              "No ambient transaction found. Ensure TransactionScope is active.");

            return GenerateGuidFromString(current.TransactionInformation.LocalIdentifier);
        }
    }

    public object GetUnderlyingTransaction()
        => Transaction.Current
           ?? throw new InvalidOperationException(
               "No ambient transaction found. Ensure TransactionScope is active.");

    private static Guid GenerateGuidFromString(string input)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
