using Microsoft.Extensions.Logging;

namespace Fake.Logging;

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
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 发送到飞书的最低日志级别（通常设置为 Warning 或 Error）
    /// </summary>
    public LogLevel FeiShuMinimumLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// 飞书通知配置
    /// </summary>
    public FeiShuNoticeOptions NotificationOptions { get; set; } = new();
}
