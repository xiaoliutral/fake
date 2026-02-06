using Microsoft.Extensions.Logging;

namespace Fake.FeiShu;

/// <summary>
/// 飞书日志记录器
/// </summary>
internal sealed class FeiShuLogger(
    string categoryName,
    Func<FeiShuLoggerConfiguration> getCurrentConfig,
    Func<FeiShuNotificationService> getNotificationService)
    : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        var config = getCurrentConfig();
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

        var message = formatter(state, exception);
        if (exception != null)
        {
            message += $"\n{exception}";
        }

        // 添加分类信息
        var fullMessage = $"[{categoryName}] {message}";
        
        getNotificationService().Enqueue(fullMessage, GetSubTitle(logLevel));
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
