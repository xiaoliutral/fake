using System.Reflection;
using Fake.AspNetCore.ExceptionHandling;
using Fake.ExceptionHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.FeiShu;

public static class FeiShuServiceCollectionExtensions
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
}