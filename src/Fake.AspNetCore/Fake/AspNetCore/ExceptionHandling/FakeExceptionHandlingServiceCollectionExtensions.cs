using Fake.ExceptionHandling;
using Fake.FeiShu;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.AspNetCore.ExceptionHandling;

public static class FakeExceptionHandlingServiceCollectionExtensions
{
    /// <summary>
    /// 添加飞书异常订阅
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddFeiShuExceptionSubscriber(this IServiceCollection services)
    {
        services.TryAddTransient<IExceptionSubscriber, FeiShuExceptionSubscriber>();
        services.AddFeiShuNotification();

        return services;
    }
    
    /// <summary>
    /// 添加Fake异常过滤器
    /// </summary>
    /// <param name="services"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static IServiceCollection AddFakeExceptionFilter(this IServiceCollection services,
        Action<FakeExceptionHandlingOptions>? action = null)
    {
        services.AddFakeFilter<FakeExceptionFilter>();
        services.Configure<FakeExceptionHandlingOptions>(options =>
        {
            // 默认情况下 dev环境才输出堆栈
            options.OutputStackTrace = services.GetInstanceOrNull<IWebHostEnvironment>()?.IsDevelopment() ?? false;
            action?.Invoke(options);
        });

        return services;
    }
}