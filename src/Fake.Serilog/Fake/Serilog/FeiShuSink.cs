using System.Globalization;
using Fake.FeiShu;
using Fake.Serilog.Sink.FeiShu;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Fake.Serilog;

internal sealed class FeiShuSink(string outputTemplate) : ILogEventSink
{
    private readonly MessageTemplateTextFormatter _messageFormatter = new(outputTemplate, CultureInfo.InvariantCulture);

    public void Emit(LogEvent logEvent)
    {
        try
        {
            var accessor = FeiShuRuntimeServiceProviderAccessor.Accessor;
            var serviceProvider = accessor?.Value;
            if (serviceProvider is null)
            {
                SelfLog.WriteLine("[FeiShuSink] IServiceProvider is not ready.");
                return;
            }

            var notifier = serviceProvider.GetService(typeof(IFeiShuNotificationService)) as IFeiShuNotificationService;
            if (notifier is null)
            {
                SelfLog.WriteLine("[FeiShuSink] IFeiShuNotificationService is not registered.");
                return;
            }

            var body = RenderTemplate(_messageFormatter, logEvent).Trim();
            notifier.Enqueue(body, ToMicrosoftLevel(logEvent.Level));
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("[FeiShuSink] Emit failed: {0}", ex);
        }
    }

    private static string RenderTemplate(MessageTemplateTextFormatter formatter, LogEvent logEvent)
    {
        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);
        return writer.ToString();
    }

    private static LogLevel ToMicrosoftLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Trace,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Critical,
        _ => LogLevel.Information
    };
}
