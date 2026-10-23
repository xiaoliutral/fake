using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Fake.Messaging.Dashboard;

internal static class MessagingDashboardStaticFiles
{
    private const string UiFileName = "dashboard.html";

    private static readonly Lazy<string> UiFilePath = new(ResolveUiFilePath);

    public static async Task WriteUiAsync(HttpContext ctx, MessagingDashboardOptions options)
    {
        var path = UiFilePath.Value;
        if (!File.Exists(path))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsync(
                $"Dashboard UI not found at '{path}'. Ensure wwwroot/{UiFileName} is copied beside the assembly.");
            return;
        }

        var html = await File.ReadAllTextAsync(path, Encoding.UTF8);

        var pathBase = ctx.Request.PathBase.HasValue
            ? ctx.Request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        var match = string.IsNullOrWhiteSpace(options.PathMatch) ? "/messaging" : options.PathMatch.Trim();
        if (!match.StartsWith('/'))
            match = "/" + match;
        match = match.TrimEnd('/');

        var apiBase = pathBase + match + "/api";
        var serviceName = string.IsNullOrWhiteSpace(options.ServiceName)
            ? Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown"
            : options.ServiceName.Trim();

        html = html
            .Replace("{{API_BASE}}", apiBase, StringComparison.Ordinal)
            .Replace("{{POLL_MS}}", options.MetricsPollIntervalMs.ToString(), StringComparison.Ordinal)
            .Replace("{{SERVICE_NAME}}", serviceName, StringComparison.Ordinal);

        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync(html);
    }

    private static string ResolveUiFilePath()
    {
        var asmDir = Path.GetDirectoryName(typeof(MessagingDashboardStaticFiles).Assembly.Location)
                     ?? AppContext.BaseDirectory;
        return Path.Combine(asmDir, "wwwroot", UiFileName);
    }
}
