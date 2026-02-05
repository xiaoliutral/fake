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
    private FeiShuNotificationService _notificationService;
    private readonly object _serviceLock = new();

    public FeiShuLoggerProvider(IOptionsMonitor<FeiShuLoggerConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _notificationService = new FeiShuNotificationService(_currentConfig.NotificationOptions);
        
        // 监听配置变化，重新创建通知服务
        _optionsChangeToken = config.OnChange(updatedConfig =>
        {
            lock (_serviceLock)
            {
                _currentConfig = updatedConfig;
                
                // 配置变化时，重新创建通知服务
                var oldService = _notificationService;
                _notificationService = new FeiShuNotificationService(updatedConfig.NotificationOptions);
                oldService?.Dispose();
            }
        });
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name =>
        {
            lock (_serviceLock)
            {
                return new FeiShuLogger(name, GetCurrentConfig, () => _notificationService);
            }
        });
    }

    private FeiShuLoggerConfiguration GetCurrentConfig() => _currentConfig;

    public void Dispose()
    {
        _loggers.Clear();
        _optionsChangeToken?.Dispose();
        
        lock (_serviceLock)
        {
            _notificationService?.Dispose();
        }
    }
}
