namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// IntegrationEventLog 根配置，对应配置节 <c>IntegrationEventLog</c>。
/// <code>
/// Configure&lt;IntegrationEventLogOptions&gt;(o =&gt;
/// {
///     o.OutboxPublisher.ScanInterval = TimeSpan.FromSeconds(5);
///     o.Inbox.ProcessingLockTimeout = TimeSpan.FromMinutes(10);
/// });
/// </code>
/// </summary>
public class IntegrationEventLogOptions
{
    public const string SectionName = "IntegrationEventLog";

    public OutboxPublisherOptions OutboxPublisher { get; set; } = new();

    public InboxOptions Inbox { get; set; } = new();

    public InboxCleanupOptions InboxCleanup { get; set; } = new();

    public void Validate()
    {
        OutboxPublisher.Validate();
        Inbox.Validate();
        InboxCleanup.Validate();
    }
}

/// <summary>
/// Outbox 发布服务配置选项
/// </summary>
public class OutboxPublisherOptions
{
    /// <summary>扫描间隔（默认 10 秒）</summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>每次扫描处理的最大事件数量（默认 100）</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 单进程内并发发布度（默认 8）。每条消息使用独立 DI Scope/DbContext；
    /// 多实例横向扩展时靠 TryMarkEventAsInProgressAsync 抢占行锁。
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 8;

    /// <summary>启动延迟时间（默认 5 秒）</summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 分布式锁超时时间（默认 5 分钟）。
    /// 实例崩溃后超过此时长，其他实例可接管该事件。
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>是否启用僵尸锁恢复（默认 true）</summary>
    public bool EnableZombieLockRecovery { get; set; } = true;

    public void Validate()
    {
        if (ScanInterval < TimeSpan.FromSeconds(1))
            throw new ArgumentException("ScanInterval must be at least 1 second", nameof(ScanInterval));

        if (BatchSize is < 1 or > 1000)
            throw new ArgumentException("BatchSize must be between 1 and 1000", nameof(BatchSize));

        if (MaxDegreeOfParallelism < 1)
            throw new ArgumentException("MaxDegreeOfParallelism must be at least 1", nameof(MaxDegreeOfParallelism));

        if (StartupDelay < TimeSpan.Zero)
            throw new ArgumentException("StartupDelay cannot be negative", nameof(StartupDelay));

        if (LockTimeout < TimeSpan.FromMinutes(1))
            throw new ArgumentException("LockTimeout must be at least 1 minute", nameof(LockTimeout));
    }
}

/// <summary>
/// Inbox 消费幂等 / 抢占锁相关配置。
/// </summary>
public class InboxOptions
{
    /// <summary>Consuming 超过此时长视为僵尸，允许重新抢占（默认 5 分钟）。</summary>
    public TimeSpan ProcessingLockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 按事件类型覆盖锁超时。Key 支持 <see cref="Type.FullName"/> 或 <see cref="Type.Name"/>。
    /// </summary>
    public Dictionary<string, TimeSpan> ProcessingLockTimeouts { get; set; } = new(StringComparer.Ordinal);

    /// <summary>解析指定事件类型的锁超时：按类型覆盖 &gt; 默认值。</summary>
    public TimeSpan ResolveProcessingLockTimeout(string? eventTypeName)
    {
        if (!string.IsNullOrWhiteSpace(eventTypeName))
        {
            if (ProcessingLockTimeouts.TryGetValue(eventTypeName, out var byKey))
                return byKey;

            var shortName = eventTypeName.Contains('.')
                ? eventTypeName[(eventTypeName.LastIndexOf('.') + 1)..]
                : eventTypeName;

            if (ProcessingLockTimeouts.TryGetValue(shortName, out var byName))
                return byName;
        }

        return ProcessingLockTimeout;
    }

    public void Validate()
    {
        if (ProcessingLockTimeout < TimeSpan.FromSeconds(30))
            throw new ArgumentException(
                "ProcessingLockTimeout must be at least 30 seconds",
                nameof(ProcessingLockTimeout));

        foreach (var (key, value) in ProcessingLockTimeouts)
        {
            if (value < TimeSpan.FromSeconds(30))
                throw new ArgumentException(
                    $"ProcessingLockTimeouts['{key}'] must be at least 30 seconds",
                    nameof(ProcessingLockTimeouts));
        }
    }
}

/// <summary>
/// Inbox 清理服务配置选项
/// </summary>
public class InboxCleanupOptions
{
    /// <summary>保留天数（默认 7 天）</summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>清理间隔（默认每天一次）</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>启动延迟时间（默认 1 分钟）</summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromMinutes(1);

    public void Validate()
    {
        if (RetentionDays < 1)
            throw new ArgumentException("RetentionDays must be at least 1 day", nameof(RetentionDays));

        if (CleanupInterval < TimeSpan.FromMinutes(1))
            throw new ArgumentException("CleanupInterval must be at least 1 minute", nameof(CleanupInterval));

        if (StartupDelay < TimeSpan.Zero)
            throw new ArgumentException("StartupDelay cannot be negative", nameof(StartupDelay));
    }
}
