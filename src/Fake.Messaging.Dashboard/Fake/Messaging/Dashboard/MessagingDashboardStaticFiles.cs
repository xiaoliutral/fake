using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Fake.Messaging.Dashboard;

internal static class MessagingDashboardStaticFiles
{
    private const string UiFileName = "dashboard.html";

    private static readonly Assembly Assembly = typeof(MessagingDashboardStaticFiles).Assembly;
    private static readonly Lazy<string> UiHtmlResource = new(ResolveUiResourceName);

    public static async Task WriteUiAsync(HttpContext ctx, MessagingDashboardOptions options)
    {
        await using var stream = Assembly.GetManifestResourceStream(UiHtmlResource.Value);
        if (stream == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsync("Dashboard UI not found.");
            return;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var html = await reader.ReadToEndAsync();

        var pathBase = ctx.Request.PathBase.HasValue
            ? ctx.Request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        var match = string.IsNullOrWhiteSpace(options.PathMatch) ? "/messaging" : options.PathMatch.Trim();
        if (!match.StartsWith('/'))
            match = "/" + match;
        match = match.TrimEnd('/');

        var apiBase = pathBase + match + "/api";
        html = html
            .Replace("{{API_BASE}}", apiBase, StringComparison.Ordinal)
            .Replace("{{POLL_MS}}", options.MetricsPollIntervalMs.ToString(), StringComparison.Ordinal);

        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync(html);
    }

    private static string ResolveUiResourceName()
    {
        var name = Assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(UiFileName, StringComparison.OrdinalIgnoreCase));

        return name ?? throw new InvalidOperationException(
            $"Embedded resource wwwroot/{UiFileName} was not found in Fake.Messaging.Dashboard.");
    }
}
