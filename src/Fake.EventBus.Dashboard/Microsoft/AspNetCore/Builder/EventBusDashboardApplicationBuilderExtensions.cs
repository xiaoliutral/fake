using Fake.EventBus.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

public static class EventBusDashboardApplicationBuilderExtensions
{
    private const string MappedKey = "Fake.EventBus.Dashboard.Mapped";

    /// <summary>
    /// 挂载 EventBus Dashboard（UI + API）。默认路径 <c>/eventbus</c>。
    /// 若已通过 <c>AddFakeEventBusDashboard</c> 的 StartupFilter 挂载，则不会重复注册。
    /// </summary>
    public static IApplicationBuilder UseFakeEventBusDashboard(this IApplicationBuilder app)
    {
        if (app.Properties.ContainsKey(MappedKey))
            return app;

        app.Properties[MappedKey] = true;

        var options = app.ApplicationServices.GetRequiredService<IOptions<EventBusDashboardOptions>>().Value;
        var path = NormalizePath(options.PathMatch);

        app.Map(path, branch =>
        {
            branch.UseRouting();
            branch.UseEndpoints(endpoints =>
            {
                var api = branch.ApplicationServices.GetRequiredService<EventBusDashboardApi>();
                api.Map(endpoints);

                // 不用 index.html，避免与宿主 Swagger/静态站入口冲突
                endpoints.MapGet("/", async ctx =>
                {
                    await EventBusDashboardStaticFiles.WriteUiAsync(ctx, options);
                });

                endpoints.MapGet("/dashboard.html", async ctx =>
                {
                    await EventBusDashboardStaticFiles.WriteUiAsync(ctx, options);
                });
            });
        });

        return app;
    }

    private static PathString NormalizePath(string pathMatch)
    {
        if (string.IsNullOrWhiteSpace(pathMatch))
            return "/eventbus";

        return pathMatch.StartsWith('/') ? pathMatch : "/" + pathMatch;
    }
}
