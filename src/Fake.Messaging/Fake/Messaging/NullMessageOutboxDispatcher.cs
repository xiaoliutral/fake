namespace Fake.Messaging;

public sealed class NullMessageOutboxDispatcher : IMessageOutboxDispatcher
{
    public static NullMessageOutboxDispatcher Instance { get; } = new();

    public Task DispatchPendingAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DispatchByTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> DispatchOneAsync(Guid messageId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
