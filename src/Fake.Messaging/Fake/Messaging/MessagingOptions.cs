namespace Fake.Messaging;

/// <summary>
/// Messaging Outbox/Inbox 扫描与清理选项。
/// </summary>
public class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// 默认发布 Topic（交换机）。<see cref="MessageRouteAttribute.Topic"/> 为空时使用。
    /// </summary>
    public string? DefaultTopic { get; set; }

    /// <summary>
    /// 默认 Topic 类型（direct / topic …）。<see cref="MessageRouteAttribute.TopicType"/> 为空时使用。
    /// </summary>
    public string DefaultTopicType { get; set; } = "direct";

    /// <summary>Outbox 扫描间隔。</summary>
    public TimeSpan OutboxScanInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>每次扫描最多投递条数。</summary>
    public int OutboxBatchSize { get; set; } = 100;

    /// <summary>单进程并发投递度。</summary>
    public int OutboxMaxDegreeOfParallelism { get; set; } = 8;

    /// <summary>启动延迟。</summary>
    public TimeSpan OutboxStartupDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Outbox InProgress 锁超时。</summary>
    public TimeSpan OutboxLockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>是否启用僵尸锁恢复。</summary>
    public bool EnableZombieLockRecovery { get; set; } = true;

    /// <summary>Inbox Processing 锁超时，超时后可被抢占。</summary>
    public TimeSpan ProcessingLockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>按 TypeName 覆盖锁超时。</summary>
    public Dictionary<string, TimeSpan> ProcessingLockTimeouts { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Inbox 成功记录保留天数。</summary>
    public int InboxRetentionDays { get; set; } = 7;

    /// <summary>Inbox 清理扫描间隔。</summary>
    public TimeSpan InboxCleanupInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Inbox 清理启动延迟。</summary>
    public TimeSpan InboxCleanupStartupDelay { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan ResolveProcessingLockTimeout(string? typeName)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            if (ProcessingLockTimeouts.TryGetValue(typeName, out var byKey))
                return byKey;

            var shortName = typeName.Contains('.')
                ? typeName[(typeName.LastIndexOf('.') + 1)..]
                : typeName;

            if (ProcessingLockTimeouts.TryGetValue(shortName, out var byName))
                return byName;
        }

        return ProcessingLockTimeout;
    }

    public void Validate()
    {
        if (OutboxScanInterval < TimeSpan.FromSeconds(1))
            throw new ArgumentException("OutboxScanInterval must be at least 1 second");

        if (OutboxBatchSize is < 1 or > 1000)
            throw new ArgumentException("OutboxBatchSize must be between 1 and 1000");

        if (OutboxMaxDegreeOfParallelism < 1)
            throw new ArgumentException("OutboxMaxDegreeOfParallelism must be at least 1");

        if (OutboxLockTimeout < TimeSpan.FromMinutes(1))
            throw new ArgumentException("OutboxLockTimeout must be at least 1 minute");

        if (ProcessingLockTimeout < TimeSpan.FromSeconds(30))
            throw new ArgumentException("ProcessingLockTimeout must be at least 30 seconds");

        if (InboxRetentionDays < 1)
            throw new ArgumentException("InboxRetentionDays must be at least 1");

        if (InboxCleanupInterval < TimeSpan.FromMinutes(1))
            throw new ArgumentException("InboxCleanupInterval must be at least 1 minute");
    }
}
