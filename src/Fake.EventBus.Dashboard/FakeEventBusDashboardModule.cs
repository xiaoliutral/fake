using Fake.EventBus;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.EventBus.Dashboard;

/// <summary>
/// 独立 EventBus Dashboard 模块。也可不依赖 Fake 模块系统，直接：
/// <code>
/// services.AddFakeEventBusDashboard(o => o.PathMatch = "/eventbus");
/// </code>
/// </summary>
[DependsOn(typeof(FakeEventBusModule))]
public class FakeEventBusDashboardModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<EventBusDashboardOptions>(
            configuration.GetSection("EventBus:Dashboard"));
        context.Services.AddFakeEventBusDashboard();
    }
}
