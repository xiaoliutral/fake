using Fake.EventBus.Dashboard;
using Fake.EventBus.Distributed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventBusDashboardServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EventBus Dashboard（CAP 风格：可插拔、不绑死宿主应用）。
    /// 默认通过 <see cref="IStartupFilter"/> 自动挂载到管道；也可显式调用 <c>UseFakeEventBusDashboard()</c>。
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
        services.TryAddSingleton<IDlxAdminService>(_ => NullDlxAdminService.Instance);

        services.TryAddSingleton<EventBusDashboardApi>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStartupFilter, EventBusDashboardStartupFilter>());
        return services;
    }
}
