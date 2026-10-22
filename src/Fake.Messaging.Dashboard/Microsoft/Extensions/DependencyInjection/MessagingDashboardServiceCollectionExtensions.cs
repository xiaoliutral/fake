using Fake.Messaging;
using Fake.Messaging.Admin;
using Fake.Messaging.Dashboard;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingDashboardServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Messaging Dashboard 服务。
    /// 宿主须在 <c>UsePathBase</c> 之后调用 <c>UseFakeMessagingDashboard()</c>。
    /// </summary>
    public static IServiceCollection AddFakeMessagingDashboard(
        this IServiceCollection services,
        Action<MessagingDashboardOptions>? configure = null)
    {
        services.AddOptions<MessagingDashboardOptions>();
        if (configure != null)
            services.Configure(configure);

        // 未接入 Messaging.EF / EventBus 适配时也能启动（空数据 / Null 运维实现）
        services.TryAddTransient<IMessageOutboxAdminStore>(_ => NullMessageOutboxAdminStore.Instance);
        services.TryAddTransient<IMessageInboxAdminStore>(_ => NullMessageInboxAdminStore.Instance);
        services.TryAddSingleton<IMessageOutboxDispatcher>(_ => NullMessageOutboxDispatcher.Instance);

        services.TryAddScoped<MessagingDashboardApi>();
        return services;
    }
}
