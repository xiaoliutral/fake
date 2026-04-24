using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fake.FeiShu;

public static class FeiShuServiceCollectionExtensions
{
    public static IServiceCollection AddFeiShuNotification(this IServiceCollection services)
    {
        var configuration = services.GetConfiguration();
        services.Configure<FeiShuNoticeOptions>(configuration.GetSection("FeiShuNotice"));
        services.Configure<FeiShuNoticeOptions>(options =>
        {
            options.Title = options.Title.IsNullOrWhiteSpace()
                ? Assembly.GetEntryAssembly()?.GetName().Name ?? ""
                : options.Title;
        });
        services.TryAddSingleton<IFeiShuNotificationService, FeiShuNotificationService>();

        return services;
    }
}