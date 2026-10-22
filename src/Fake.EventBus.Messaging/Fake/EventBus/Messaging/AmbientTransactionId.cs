using System.Transactions;

namespace Fake.EventBus.Messaging;

internal static class AmbientTransactionId
{
    public static Guid Current
    {
        get
        {
            var current = Transaction.Current
                          ?? throw new InvalidOperationException(
                              "No ambient transaction found. Ensure TransactionScope is active.");
            var hash = System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes(current.TransactionInformation.LocalIdentifier));
            return new Guid(hash);
        }
    }
}
