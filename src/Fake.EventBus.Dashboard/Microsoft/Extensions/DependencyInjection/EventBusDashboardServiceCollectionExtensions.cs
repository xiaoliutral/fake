using Fake.EventBus.Dashboard;
using Fake.EventBus.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventBusDashboardServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EventBus Dashboard 服务。
    /// 宿主须在 <c>UsePathBase</c> 之后调用 <c>UseFakeEventBusDashboard()</c>
    ///（网关前缀如 /admin 时尤为重要，否则会 404）。
    /// </summary>
    public static IServiceCollection AddFakeEventBusDashboard(
        this IServiceCollection services,
        Action<EventBusDashboardOptions>? configure = null)
    {
        services.AddOptions<EventBusDashboardOptions>();
        if (configure != null)
            services.Configure(configure);

        // 未接入 EventBus 模块时也能启动 Dashboard（空数据 / Null 运维实现）
        services.TryAddSingleton<EventBusMetrics>();
        services.TryAddTransient<IOutboxAdminStore>(_ => NullOutboxAdminStore.Instance);
        services.TryAddTransient<IInboxAdminStore>(_ => NullInboxAdminStore.Instance);
        services.TryAddTransient<IOutboxEventPublisher>(_ => NullOutboxEventPublisher.Instance);
        services.TryAddTransient<IInboxEventReprocessor>(_ => NullInboxEventReprocessor.Instance);
        services.TryAddSingleton<IEventBusTopology>(_ => NullEventBusTopology.Instance);
        services.TryAddSingleton<IDlxAdminService>(_ => NullDlxAdminService.Instance);

        services.TryAddScoped<EventBusDashboardApi>();
        return services;
    }
}

