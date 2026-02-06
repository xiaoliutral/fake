using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Fake.FeiShu;

/// <summary>
/// 飞书日志扩展方法
/// </summary>
public static class FeiShuLoggerExtensions
{
    /// <summary>
    /// 添加飞书日志提供程序
    /// </summary>
    public static ILoggingBuilder AddFeiShu(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, FeiShuLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <FeiShuLoggerConfiguration, FeiShuLoggerProvider>(builder.Services);

        return builder;
    }

    /// <summary>
    /// 添加飞书日志提供程序（带配置）
    /// </summary>
    public static ILoggingBuilder AddFeiShu(
        this ILoggingBuilder builder,
        Action<FeiShuLoggerConfiguration> configure)
    {
        builder.AddFeiShu();
        builder.Services.Configure(configure);
        return builder;
    }
}
