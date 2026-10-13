using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Fake.EventBus.Dashboard;

/// <summary>
/// 与 CAP Dashboard 类似：注册服务后自动挂到 ASP.NET Core 管道。
/// </summary>
internal sealed class EventBusDashboardStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseFakeEventBusDashboard();
            next(app);
        };
    }
}
