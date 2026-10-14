namespace Fake.EventBus.Distributed;

public sealed class NullInboxEventReprocessor : IInboxEventReprocessor
{
    public static NullInboxEventReprocessor Instance { get; } = new();

    public Task<InboxReprocessResult> ReprocessAsync(Guid eventId, CancellationToken cancellationToken = default)
        => Task.FromResult(new InboxReprocessResult(
            false,
            "Inbox reprocessor is not available. Add Fake.EventBus.IntegrationEventLog."));
}