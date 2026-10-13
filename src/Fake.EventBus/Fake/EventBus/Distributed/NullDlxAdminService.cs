namespace Fake.EventBus.Distributed;

public sealed class NullDlxAdminService : IDlxAdminService
{
    public static NullDlxAdminService Instance { get; } = new();

    public bool IsAvailable => false;

    public Task<IReadOnlyList<DlxMessageDto>> GetMessagesAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DlxMessageDto>>(Array.Empty<DlxMessageDto>());

    public Task RequeueAsync(string messageId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("DLX admin is not available. Enable RabbitMQ EventBus with EnableDlx.");

    public Task DiscardAsync(string messageId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("DLX admin is not available. Enable RabbitMQ EventBus with EnableDlx.");
}
