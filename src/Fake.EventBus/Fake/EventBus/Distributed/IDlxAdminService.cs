namespace Fake.EventBus.Distributed;

/// <summary>
/// 死信队列运维。未接入物理总线 DLX 时为空实现。
/// </summary>
public interface IDlxAdminService
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<DlxMessageDto>> GetMessagesAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将死信重新发布到主交换机（原 routing key），并从 DLX 队列移除。
    /// </summary>
    Task RequeueAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 丢弃死信（Ack）。
    /// </summary>
    Task DiscardAsync(string messageId, CancellationToken cancellationToken = default);
}
