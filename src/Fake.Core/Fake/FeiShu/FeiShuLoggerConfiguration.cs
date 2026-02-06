using Microsoft.Extensions.Logging;

namespace Fake.FeiShu;

/// <summary>
/// 飞书日志配置
/// </summary>
public class FeiShuLoggerConfiguration
{
    /// <summary>
    /// 是否启用飞书日志
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最低日志级别（用于 ILogger.IsEnabled）
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// 飞书通知配置
    /// </summary>
    public FeiShuNoticeOptions NotificationOptions { get; set; } = new();
}
