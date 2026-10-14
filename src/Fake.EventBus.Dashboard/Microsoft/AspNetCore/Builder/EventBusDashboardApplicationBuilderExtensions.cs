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
    /// 请放在 <c>UsePathBase</c> 之后调用；重复调用会被忽略。
    /// 对外完整路径示例：PathBase=/admin → <c>/admin/eventbus</c>。
    /// </summary>
    public static IApplicationBuilder UseFakeEventBusDashboard(this IApplicationBuilder app)
    {
        if (app.Properties.ContainsKey(MappedKey))
            return app;

        app.Properties[MappedKey] = true;

        var options = app.ApplicationServices.GetRequiredService<IOptions<EventBusDashboardOptions>>().Value;
        var path = NormalizePath(options.PathMatch);

        // WebApplication / 最小宿主：走 endpoint 路由，匹配的是 PathBase 剥离后的 Path，
        // 比 IApplicationBuilder.Map 中间件分支更不容易在网关前缀下 404。
        if (app is IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(path);
            EventBusDashboardApi.MapEndpoints(group);

            group.MapGet("/", async ctx =>
            {
                await EventBusDashboardStaticFiles.WriteUiAsync(ctx, options);
            });
            group.MapGet("/dashboard.html", async ctx =>
            {
                await EventBusDashboardStaticFiles.WriteUiAsync(ctx, options);
            });

            return app;
        }

        app.Map(path, branch =>
        {
            branch.UseRouting();
            branch.UseEndpoints(branchEndpoints =>
            {
                EventBusDashboardApi.MapEndpoints(branchEndpoints);

                branchEndpoints.MapGet("/", async ctx =>
                {
                    await EventBusDashboardStaticFiles.WriteUiAsync(ctx, options);
                });

                branchEndpoints.MapGet("/dashboard.html", async ctx =>
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
