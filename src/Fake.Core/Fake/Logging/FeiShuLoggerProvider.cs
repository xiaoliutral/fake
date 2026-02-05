using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.Logging;

/// <summary>
/// 飞书日志提供程序，自动将日志发送到飞书
/// </summary>
[ProviderAlias("FeiShu")]
public sealed class FeiShuLoggerProvider : ILoggerProvider
{
    private readonly IDisposable? _optionsChangeToken;
    private FeiShuLoggerConfiguration _currentConfig;
    private readonly ConcurrentDictionary<string, FeiShuLogger> _loggers = new();
    private readonly FeiShuNotificationService _notificationService;

    public FeiShuLoggerProvider(IOptionsMonitor<FeiShuLoggerConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _optionsChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        _notificationService = new FeiShuNotificationService(_currentConfig.NotificationOptions);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FeiShuLogger(name, GetCurrentConfig, _notificationService));
    }

    private FeiShuLoggerConfiguration GetCurrentConfig() => _currentConfig;

    public void Dispose()
    {
        _loggers.Clear();
        _optionsChangeToken?.Dispose();
        _notificationService.Dispose();
    }
}
