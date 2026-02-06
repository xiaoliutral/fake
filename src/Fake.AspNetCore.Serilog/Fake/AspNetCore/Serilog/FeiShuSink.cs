using Fake.FeiShu;
using Serilog.Core;
using Serilog.Events;

namespace Fake.AspNetCore.Serilog;

/// <summary>
/// Serilog Sink，将日志发送到飞书
/// </summary>
public class FeiShuSink(
    FeiShuNoticeOptions options,
    IFormatProvider? formatProvider = null,
    LogEventLevel minimumLevel = LogEventLevel.Warning,
    string? outputTemplate = null)
    : ILogEventSink
{
    private readonly FeiShuNotificationService _notificationService = new(options);

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < minimumLevel)
            return;

// #if DEBUG
//         // Debug 模式不发送
//         return;
// #endif

        var message = logEvent.RenderMessage(formatProvider);
        
        // 如果有异常，附加异常信息
        if (logEvent.Exception != null)
        {
            message += $"\n{logEvent.Exception}";
        }

        // 添加结构化属性（如果有）
        if (logEvent.Properties.Count > 0)
        {
            var properties = string.Join(", ", logEvent.Properties.Select(p => 
                $"{p.Key}={p.Value}"));
            message += $"\n属性: {properties}";
        }

        var subTitle = GetSubTitle(logEvent.Level);
        _notificationService.Enqueue(message, subTitle);
    }

    private string GetSubTitle(LogEventLevel level) => level switch
    {
        LogEventLevel.Fatal => "Critical",
        LogEventLevel.Error => "Error",
        LogEventLevel.Warning => "Warn",
        LogEventLevel.Information => "Info",
        _ => "Debug"
    };
}
