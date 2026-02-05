using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Fake.Logging.Serilog;

/// <summary>
/// Serilog 飞书扩展方法
/// </summary>
public static class FeiShuSinkExtensions
{
    /// <summary>
    /// 添加飞书 Sink
    /// </summary>
    public static LoggerConfiguration FeiShu(
        this LoggerSinkConfiguration sinkConfiguration,
        FeiShuNoticeOptions options,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Warning,
        IFormatProvider? formatProvider = null)
    {
        if (sinkConfiguration == null)
            throw new ArgumentNullException(nameof(sinkConfiguration));
        
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.Validate();

        return sinkConfiguration.Sink(
            new FeiShuSink(options, formatProvider, restrictedToMinimumLevel),
            restrictedToMinimumLevel);
    }

    /// <summary>
    /// 添加飞书 Sink（使用配置委托）
    /// </summary>
    public static LoggerConfiguration FeiShu(
        this LoggerSinkConfiguration sinkConfiguration,
        Action<FeiShuNoticeOptions> configure,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Warning,
        IFormatProvider? formatProvider = null)
    {
        var options = new FeiShuNoticeOptions();
        configure(options);
        
        return sinkConfiguration.FeiShu(options, restrictedToMinimumLevel, formatProvider);
    }
}
