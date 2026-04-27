using System.Text;
using Fake.Logging;

namespace Microsoft.Extensions.Logging;

public static class FakeLoggerExtensions
{
    /// <summary>
    /// 异常日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception"></param>
    public static void LogException(this ILogger logger, Exception exception)
    {
        var logLevel = exception.GetLogLevel();

        logger.LogException(exception, logLevel);
    }

    /// <summary>
    /// 异常日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception"></param>
    /// <param name="logLevel"></param>
    public static void LogException(this ILogger logger, Exception exception, LogLevel logLevel)
    {
        logger.LogWithLevel(logLevel, exception);
        LogExceptionLog(logger, exception);
        LogData(logger, exception, logLevel);
    }

    private static void LogExceptionLog(ILogger logger, Exception exception)
    {
        var loggingExceptions = new List<IHasExceptionLog>();

        switch (exception)
        {
            case IHasExceptionLog logging:
                loggingExceptions.Add(logging);
                break;
            case AggregateException { InnerException: not null } aggException:
            {
                if (aggException.InnerException is IHasExceptionLog selfLogging)
                {
                    loggingExceptions.Add(selfLogging);
                }

                foreach (var innerException in aggException.InnerExceptions)
                {
                    if (innerException is IHasExceptionLog withSelfLogging)
                    {
                        loggingExceptions.TryAdd(withSelfLogging);
                    }
                }

                break;
            }
        }

        foreach (var ex in loggingExceptions)
        {
            ex.Log(logger);
        }
    }

    /// <summary>
    /// 自定义级别异常日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    public static void LogWithLevel(this ILogger logger, LogLevel logLevel, Exception? exception = null,
        string? message = null)
    {
        switch (logLevel)
        {
            case LogLevel.Critical:
                logger.LogCritical(exception, message ?? exception?.Message);
                break;
            case LogLevel.Error:
                logger.LogError(exception, message ?? exception?.Message);
                break;
            case LogLevel.Warning:
                logger.LogWarning(exception, message ?? exception?.Message);
                break;
            case LogLevel.Information:
                logger.LogInformation(exception, message ?? exception?.Message);
                break;
            case LogLevel.Debug:
                logger.LogDebug(exception, message ?? exception?.Message);
                break;
            case LogLevel.Trace:
            case LogLevel.None:
            default: // LogLevel.Trace || LogLevel.None
                logger.LogDebug(exception, message ?? exception?.Message);
                break;
        }
    }

    /// <summary>
    /// 自定义级别日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    public static void LogWithLevel(this ILogger logger, LogLevel logLevel, string message)
    {
        switch (logLevel)
        {
            case LogLevel.Critical:
                logger.LogCritical(message);
                break;
            case LogLevel.Error:
                logger.LogError(message);
                break;
            case LogLevel.Warning:
                logger.LogWarning(message);
                break;
            case LogLevel.Information:
                logger.LogInformation(message);
                break;
            case LogLevel.Debug:
                logger.LogDebug(message);
                break;
            case LogLevel.Trace:
            case LogLevel.None:
            default: // LogLevel.Trace || LogLevel.None
                logger.LogDebug(message);
                break;
        }
    }
    
    private static void LogData(ILogger logger, Exception exception, LogLevel logLevel)
    {
        if (exception.Data.Count <= 0)
        {
            return;
        }

        var exceptionData = new StringBuilder();
        exceptionData.AppendLine("---------- Exception Data ----------");
        foreach (var key in exception.Data.Keys)
        {
            exceptionData.AppendLine($"{key} = {exception.Data[key]}");
        }

        logger.LogWithLevel(logLevel, exceptionData.ToString());
    }

}