namespace Fake.EntityFrameworkCore.IntegrationEventLog.Options;

/// <summary>
/// Outbox 发布服务配置选项
/// </summary>
public class OutboxPublisherOptions
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string SectionName = "IntegrationEventLog:OutboxPublisher";

    /// <summary>
    /// 扫描间隔（默认 10 秒）
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 每次扫描处理的最大事件数量（批量大小，默认 100）
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 启动延迟时间（默认 5 秒）
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 分布式锁超时时间（默认 5 分钟）
    /// 如果实例崩溃，超过此时间后其他实例可以接管该事件
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 是否启用僵尸锁恢复（默认 true）
    /// </summary>
    public bool EnableZombieLockRecovery { get; set; } = true;

    /// <summary>
    /// 验证配置
    /// </summary>
    public void Validate()
    {
        if (ScanInterval < TimeSpan.FromSeconds(1))
            throw new ArgumentException("ScanInterval must be at least 1 second", nameof(ScanInterval));

        if (BatchSize < 1)
            throw new ArgumentException("BatchSize must be at least 1", nameof(BatchSize));

        if (BatchSize > 1000)
            throw new ArgumentException("BatchSize cannot exceed 1000", nameof(BatchSize));

        if (StartupDelay < TimeSpan.Zero)
            throw new ArgumentException("StartupDelay cannot be negative", nameof(StartupDelay));

        if (LockTimeout < TimeSpan.FromMinutes(1))
            throw new ArgumentException("LockTimeout must be at least 1 minute", nameof(LockTimeout));
    }
}
