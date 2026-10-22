using Fake.Messaging.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

public static class MessagingDashboardApplicationBuilderExtensions
{
    private const string MappedKey = "Fake.Messaging.Dashboard.Mapped";

    /// <summary>
    /// 挂载 Messaging Dashboard（UI + API）。默认路径 <c>/messaging</c>。
    /// 请放在 <c>UsePathBase</c> 之后调用；重复调用会被忽略。
    /// </summary>
    public static IApplicationBuilder UseFakeMessagingDashboard(this IApplicationBuilder app)
    {
        if (app.Properties.ContainsKey(MappedKey))
            return app;

        app.Properties[MappedKey] = true;

        var options = app.ApplicationServices.GetRequiredService<IOptions<MessagingDashboardOptions>>().Value;
        var path = NormalizePath(options.PathMatch);

        if (app is IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(path);
            MessagingDashboardApi.MapEndpoints(group);

            group.MapGet("/", async ctx =>
            {
                await MessagingDashboardStaticFiles.WriteUiAsync(ctx, options);
            });
            group.MapGet("/dashboard.html", async ctx =>
            {
                await MessagingDashboardStaticFiles.WriteUiAsync(ctx, options);
            });

            return app;
        }

        app.Map(path, branch =>
        {
            branch.UseRouting();
            branch.UseEndpoints(branchEndpoints =>
            {
                MessagingDashboardApi.MapEndpoints(branchEndpoints);

                branchEndpoints.MapGet("/", async ctx =>
                {
                    await MessagingDashboardStaticFiles.WriteUiAsync(ctx, options);
                });

                branchEndpoints.MapGet("/dashboard.html", async ctx =>
                {
                    await MessagingDashboardStaticFiles.WriteUiAsync(ctx, options);
                });
            });
        });

        return app;
    }

    private static PathString NormalizePath(string pathMatch)
    {
        if (string.IsNullOrWhiteSpace(pathMatch))
            return "/messaging";

        return pathMatch.StartsWith('/') ? pathMatch : "/" + pathMatch;
    }
}
