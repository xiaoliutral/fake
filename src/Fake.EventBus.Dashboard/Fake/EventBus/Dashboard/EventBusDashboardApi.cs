using System.Text.Json;
using System.Text.Json.Serialization;
using Fake.EventBus.Distributed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Fake.EventBus.Dashboard;

public sealed class EventBusDashboardApi(
    EventBusMetrics metrics,
    IOutboxAdminStore outboxStore,
    IInboxAdminStore inboxStore,
    IOutboxEventPublisher outboxPublisher,
    IDlxAdminService dlxAdmin,
    IOptions<EventBusDashboardOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/stats", async ctx =>
        {
            var snapshot = metrics.GetSnapshot();
            var outboxCounts = await outboxStore.GetStateCountsAsync(ctx.RequestAborted);
            var inboxCounts = await inboxStore.GetStateCountsAsync(ctx.RequestAborted);

            await WriteJsonAsync(ctx, new
            {
                metrics = snapshot,
                outbox = outboxCounts,
                inbox = inboxCounts,
                dlxAvailable = dlxAdmin.IsAvailable,
                pollIntervalMs = options.Value.MetricsPollIntervalMs,
                serverTimeUtc = DateTime.UtcNow
            });
        });

        group.MapGet("/metrics", async ctx =>
        {
            await WriteJsonAsync(ctx, metrics.GetSnapshot());
        });

        group.MapGet("/outbox", async ctx =>
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
        });

        group.MapGet("/outbox/{eventId:guid}", async (HttpContext ctx, Guid eventId) =>
        {
            var item = await outboxStore.FindAsync(eventId, ctx.RequestAborted);
            if (item == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await WriteJsonAsync(ctx, item);
        });

        group.MapPost("/outbox/{eventId:guid}/retry", async (HttpContext ctx, Guid eventId) =>
        {
            var body = await ReadJsonAsync<OutboxRetryRequest>(ctx) ?? new OutboxRetryRequest();
            await outboxStore.ResetForRetryAsync(eventId, ctx.RequestAborted);

            if (body.PublishNow)
                await outboxPublisher.PublishOneAsync(eventId, ctx.RequestAborted);

            await WriteJsonAsync(ctx, new
            {
                eventId,
                reset = true,
                published = body.PublishNow
            });
        });

        group.MapGet("/inbox", async ctx =>
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
        });

        group.MapGet("/inbox/{eventId:guid}", async (HttpContext ctx, Guid eventId) =>
        {
            var item = await inboxStore.FindAsync(eventId, ctx.RequestAborted);
            if (item == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await WriteJsonAsync(ctx, item);
        });

        group.MapPost("/inbox/{eventId:guid}/retry", async (HttpContext ctx, Guid eventId) =>
        {
            await inboxStore.ResetForRetryAsync(eventId, ctx.RequestAborted);
            await WriteJsonAsync(ctx, new { eventId, reset = true });
        });

        group.MapGet("/dlx/messages", async ctx =>
        {
            var maxCount = ParseInt(ctx.Request.Query["maxCount"], 50);
            await WriteJsonAsync(ctx, new
            {
                available = dlxAdmin.IsAvailable,
                items = await dlxAdmin.GetMessagesAsync(maxCount, ctx.RequestAborted)
            });
        });

        group.MapPost("/dlx/messages/{messageId}/requeue", async (HttpContext ctx, string messageId) =>
        {
            await dlxAdmin.RequeueAsync(messageId, ctx.RequestAborted);
            await WriteJsonAsync(ctx, new { messageId, requeued = true });
        });

        group.MapPost("/dlx/messages/{messageId}/discard", async (HttpContext ctx, string messageId) =>
        {
            await dlxAdmin.DiscardAsync(messageId, ctx.RequestAborted);
            await WriteJsonAsync(ctx, new { messageId, discarded = true });
        });
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
