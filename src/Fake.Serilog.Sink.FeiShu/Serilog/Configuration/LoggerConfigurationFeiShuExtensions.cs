using Fake.Serilog.Sink.FeiShu;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Configuration;

public static class LoggerConfigurationFeiShuExtensions
{
    /// <summary>
    /// 当你在配置里写 "Name": "FeiShu"，Serilog 会去找 namespace Serilog 下的扩展方法 FeiShu(...)。
    /// LoggerConfigurationFeiShuExtensions.FeiShu(...) 被找到并调用
    /// </summary>
    /// <param name="sinkConfiguration"></param>
    /// <param name="minimumLogLevel"></param>
    /// <param name="outputTemplate"></param>
    /// <param name="levelSwitch"></param>
    /// <returns></returns>
    public static LoggerConfiguration FeiShu(
        this LoggerSinkConfiguration sinkConfiguration,
        LogEventLevel minimumLogLevel = LogEventLevel.Error,
        string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
        LoggingLevelSwitch? levelSwitch = null)
    {
        ArgumentNullException.ThrowIfNull(sinkConfiguration);

        return sinkConfiguration.Sink(
            new FeiShuSink(outputTemplate),
            minimumLogLevel,
            levelSwitch);
    }
}
