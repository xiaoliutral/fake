using System.Text.Json;
using System.Text.Json.Serialization;
using Fake.EventBus.Distributed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fake.EventBus.Dashboard;

public sealed class EventBusDashboardApi(
    EventBusMetrics metrics,
    IOutboxAdminStore outboxStore,
    IInboxAdminStore inboxStore,
    IOutboxEventPublisher outboxPublisher,
    IInboxEventReprocessor inboxReprocessor,
    IEventBusTopology topology,
    IDlxAdminService dlxAdmin,
    IOptions<EventBusDashboardOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // 与 Dashboard 筛选下拉的 PascalCase 状态名保持一致
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// 注册 API 路由。处理函数每请求从 <see cref="HttpContext.RequestServices"/> 解析本类型。
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/stats", async (HttpContext ctx) =>
            await Resolve(ctx).HandleStatsAsync(ctx));
        group.MapGet("/metrics", async (HttpContext ctx) =>
            await Resolve(ctx).HandleMetricsAsync(ctx));
        group.MapGet("/subscriptions", async (HttpContext ctx) =>
            await Resolve(ctx).HandleSubscriptionsAsync(ctx));

        group.MapGet("/outbox", async (HttpContext ctx) =>
            await Resolve(ctx).HandleOutboxQueryAsync(ctx));
        group.MapGet("/outbox/{eventId:guid}", async (HttpContext ctx, Guid eventId) =>
            await Resolve(ctx).HandleOutboxFindAsync(ctx, eventId));
        group.MapPost("/outbox/{eventId:guid}/retry", async (HttpContext ctx, Guid eventId) =>
            await Resolve(ctx).HandleOutboxRetryAsync(ctx, eventId));

        group.MapGet("/inbox", async (HttpContext ctx) =>
            await Resolve(ctx).HandleInboxQueryAsync(ctx));
        group.MapGet("/inbox/{eventId:guid}", async (HttpContext ctx, Guid eventId) =>
            await Resolve(ctx).HandleInboxFindAsync(ctx, eventId));
        group.MapPost("/inbox/{eventId:guid}/retry", async (HttpContext ctx, Guid eventId) =>
            await Resolve(ctx).HandleInboxRetryAsync(ctx, eventId));

        group.MapGet("/dlx/messages", async (HttpContext ctx) =>
            await Resolve(ctx).HandleDlxListAsync(ctx));
        group.MapPost("/dlx/messages/{messageId}/requeue", async (HttpContext ctx, string messageId) =>
            await Resolve(ctx).HandleDlxRequeueAsync(ctx, messageId));
        group.MapPost("/dlx/messages/{messageId}/discard", async (HttpContext ctx, string messageId) =>
            await Resolve(ctx).HandleDlxDiscardAsync(ctx, messageId));
    }

    private static EventBusDashboardApi Resolve(HttpContext ctx)
        => ctx.RequestServices.GetRequiredService<EventBusDashboardApi>();

    private async Task HandleStatsAsync(HttpContext ctx)
    {
        await WriteJsonAsync(ctx, new
        {
            metrics = metrics.GetSnapshot(),
            outbox = await outboxStore.GetStateCountsAsync(ctx.RequestAborted),
            inbox = await inboxStore.GetStateCountsAsync(ctx.RequestAborted),
            dlxAvailable = dlxAdmin.IsAvailable,
            pollIntervalMs = options.Value.MetricsPollIntervalMs,
            serverTimeUtc = DateTime.UtcNow
        });
    }

    private async Task HandleMetricsAsync(HttpContext ctx)
        => await WriteJsonAsync(ctx, metrics.GetSnapshot());

    private async Task HandleSubscriptionsAsync(HttpContext ctx)
        => await WriteJsonAsync(ctx, new { items = topology.GetSubscriptions() });

    private async Task HandleOutboxQueryAsync(HttpContext ctx)
    {
        var request = new OutboxQueryRequest
        {
            State = ParseState(ctx.Request.Query["state"]),
            EventType = ctx.Request.Query["eventType"],
            Keyword = ctx.Request.Query["keyword"],
            PageIndex = ParseInt(ctx.Request.Query["pageIndex"], 1),
            PageSize = ParseInt(ctx.Request.Query["pageSize"], 20)
        };
        await WriteJsonAsync(ctx, await outboxStore.QueryAsync(request, ctx.RequestAborted));
    }

    private async Task HandleOutboxFindAsync(HttpContext ctx, Guid eventId)
    {
        var item = await outboxStore.FindAsync(eventId, ctx.RequestAborted);
        if (item == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteJsonAsync(ctx, item);
    }

    private async Task HandleOutboxRetryAsync(HttpContext ctx, Guid eventId)
    {
        var body = await ReadJsonAsync<OutboxRetryRequest>(ctx) ?? new OutboxRetryRequest();
        try
        {
            await outboxStore.ResetForRetryAsync(eventId, ctx.RequestAborted);

            var inboxCleared = false;
            if (body.ClearInbox)
                inboxCleared = await inboxStore.TryResetForRetryAsync(eventId, ctx.RequestAborted);

            OutboxPublishOneResult? publishResult = null;
            if (body.PublishNow)
                publishResult = await outboxPublisher.PublishOneAsync(eventId, ctx.RequestAborted);

            await WriteJsonAsync(ctx, new
            {
                eventId,
                reset = true,
                inboxCleared,
                publishNow = body.PublishNow,
                published = publishResult?.Published ?? false,
                error = publishResult?.Error,
                state = publishResult?.State
            });
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(ctx, new { eventId, error = ex.Message, published = false });
        }
    }

    private async Task HandleInboxQueryAsync(HttpContext ctx)
    {
        var request = new InboxQueryRequest
        {
            State = ParseState(ctx.Request.Query["state"]),
            EventType = ctx.Request.Query["eventType"],
            Keyword = ctx.Request.Query["keyword"],
            PageIndex = ParseInt(ctx.Request.Query["pageIndex"], 1),
            PageSize = ParseInt(ctx.Request.Query["pageSize"], 20)
        };
        await WriteJsonAsync(ctx, await inboxStore.QueryAsync(request, ctx.RequestAborted));
    }

    private async Task HandleInboxFindAsync(HttpContext ctx, Guid eventId)
    {
        var item = await inboxStore.FindAsync(eventId, ctx.RequestAborted);
        if (item == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteJsonAsync(ctx, item);
    }

    private async Task HandleInboxRetryAsync(HttpContext ctx, Guid eventId)
    {
        var body = await ReadJsonAsync<InboxRetryRequest>(ctx) ?? new InboxRetryRequest();
        try
        {
            await inboxStore.ResetForRetryAsync(eventId, ctx.RequestAborted);

            InboxReprocessResult? reprocess = null;
            if (body.ReprocessNow)
                reprocess = await inboxReprocessor.ReprocessAsync(eventId, ctx.RequestAborted);

            await WriteJsonAsync(ctx, new
            {
                eventId,
                reset = true,
                reprocessNow = body.ReprocessNow,
                processed = reprocess?.Processed ?? false,
                error = reprocess?.Error,
                state = reprocess?.State
            });
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(ctx, new { eventId, error = ex.Message, processed = false });
        }
    }

    private async Task HandleDlxListAsync(HttpContext ctx)
    {
        var maxCount = ParseInt(ctx.Request.Query["maxCount"], 50);
        await WriteJsonAsync(ctx, new
        {
            available = dlxAdmin.IsAvailable,
            items = await dlxAdmin.GetMessagesAsync(maxCount, ctx.RequestAborted)
        });
    }

    private async Task HandleDlxRequeueAsync(HttpContext ctx, string messageId)
    {
        await dlxAdmin.RequeueAsync(messageId, ctx.RequestAborted);
        await WriteJsonAsync(ctx, new { messageId, requeued = true });
    }

    private async Task HandleDlxDiscardAsync(HttpContext ctx, string messageId)
    {
        await dlxAdmin.DiscardAsync(messageId, ctx.RequestAborted);
        await WriteJsonAsync(ctx, new { messageId, discarded = true });
    }

    private static async Task WriteJsonAsync(HttpContext ctx, object payload)
    {
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, payload, JsonOptions, ctx.RequestAborted);
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpContext ctx)
    {
        if (ctx.Request.ContentLength is null or 0)
            return default;

        return await JsonSerializer.DeserializeAsync<T>(ctx.Request.Body, JsonOptions, ctx.RequestAborted);
    }

    private static EventState? ParseState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<EventState>(value, ignoreCase: true, out var state) ? state : null;
    }

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(value, out var n) ? n : fallback;
}
