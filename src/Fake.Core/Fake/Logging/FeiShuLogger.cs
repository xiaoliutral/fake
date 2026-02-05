using Microsoft.Extensions.Logging;

namespace Fake.Logging;

/// <summary>
/// 飞书日志记录器
/// </summary>
internal sealed class FeiShuLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Func<FeiShuLoggerConfiguration> _getCurrentConfig;
    private readonly Func<FeiShuNotificationService> _getNotificationService;

    public FeiShuLogger(
        string categoryName,
        Func<FeiShuLoggerConfiguration> getCurrentConfig,
        Func<FeiShuNotificationService> getNotificationService)
    {
        _categoryName = categoryName;
        _getCurrentConfig = getCurrentConfig;
        _getNotificationService = getNotificationService;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel)
    {
        var config = _getCurrentConfig();
        return config.IsEnabled && logLevel >= config.MinimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var config = _getCurrentConfig();
        
        // 检查是否需要发送到飞书
        if (!ShouldSendToFeiShu(logLevel, config))
            return;

        var message = formatter(state, exception);
        if (exception != null)
        {
            message += $"\n{exception}";
        }

        // 添加分类信息
        var fullMessage = $"[{_categoryName}] {message}";
        
        _getNotificationService().Enqueue(fullMessage, GetSubTitle(logLevel));
    }

    private bool ShouldSendToFeiShu(LogLevel logLevel, FeiShuLoggerConfiguration config)
    {
#if DEBUG
        // Debug 模式不发送
        return false;
#endif
        // 根据配置的最低级别决定是否发送
        return logLevel >= config.FeiShuMinimumLevel;
    }

    private string GetSubTitle(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Critical => "Critical",
        LogLevel.Error => "Error",
        LogLevel.Warning => "Warn",
        LogLevel.Information => "Info",
        _ => "Debug"
    };
}
