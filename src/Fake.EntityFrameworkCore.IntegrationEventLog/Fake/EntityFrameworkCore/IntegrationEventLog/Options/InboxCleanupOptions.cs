namespace Fake.EntityFrameworkCore.IntegrationEventLog.Options;

/// <summary>
/// Inbox 清理服务配置选项
/// </summary>
public class InboxCleanupOptions
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string SectionName = "IntegrationEventLog:InboxCleanup";

    /// <summary>
    /// 保留天数（默认 7 天）
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// 清理间隔（默认每天一次）
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// 启动延迟时间（默认 1 分钟）
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 验证配置
    /// </summary>
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
