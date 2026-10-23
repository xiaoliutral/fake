using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fake.Messaging;
using Fake.Messaging.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.Dashboard;

public sealed class MessagingDashboardApi(
    IMessageOutboxAdminStore outboxStore,
    IMessageInboxAdminStore inboxStore,
    IMessageOutboxDispatcher outboxDispatcher,
    IOptions<MessagingDashboardOptions> options,
    IServiceProvider services)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

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
        group.MapGet("/outbox/{messageId:guid}", async (HttpContext ctx, Guid messageId) =>
            await Resolve(ctx).HandleOutboxFindAsync(ctx, messageId));
        group.MapPost("/outbox/{messageId:guid}/retry", async (HttpContext ctx, Guid messageId) =>
            await Resolve(ctx).HandleOutboxRetryAsync(ctx, messageId));

        group.MapGet("/inbox", async (HttpContext ctx) =>
            await Resolve(ctx).HandleInboxQueryAsync(ctx));
        group.MapGet("/inbox/{messageId:guid}", async (HttpContext ctx, Guid messageId) =>
            await Resolve(ctx).HandleInboxFindAsync(ctx, messageId));
        group.MapPost("/inbox/{messageId:guid}/retry", async (HttpContext ctx, Guid messageId) =>
            await Resolve(ctx).HandleInboxRetryAsync(ctx, messageId));

        group.MapGet("/dlx/messages", async (HttpContext ctx) =>
            await Resolve(ctx).HandleDlxListAsync(ctx));
        group.MapPost("/dlx/messages/{messageId}/requeue", async (HttpContext ctx, string messageId) =>
            await Resolve(ctx).HandleDlxRequeueAsync(ctx, messageId));
        group.MapPost("/dlx/messages/{messageId}/discard", async (HttpContext ctx, string messageId) =>
            await Resolve(ctx).HandleDlxDiscardAsync(ctx, messageId));
    }

    private static MessagingDashboardApi Resolve(HttpContext ctx)
        => ctx.RequestServices.GetRequiredService<MessagingDashboardApi>();

    private async Task HandleStatsAsync(HttpContext ctx)
    {
        var metrics = services.GetService<IMessagingDashboardMetricsProvider>();
        var dlx = services.GetService<IMessagingDlxAdminService>();
        var hasSubscriptions = services.GetServices<IMessagingDashboardSubscriptionsProvider>().Any();
        var reprocessor = services.GetService<IMessagingInboxLocalReprocessor>();
        var kind = ParseKind(ctx.Request.Query["kind"]);

        var serviceName = string.IsNullOrWhiteSpace(options.Value.ServiceName)
            ? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown"
            : options.Value.ServiceName.Trim();

        await WriteJsonAsync(ctx, new
        {
            serviceName,
            features = new
            {
                metrics = metrics != null,
                subscriptions = hasSubscriptions,
                dlx = dlx != null,
                inboxReprocess = reprocessor != null
            },
            metrics = metrics?.GetSnapshot(),
            outbox = await outboxStore.GetStateCountsAsync(kind, ctx.RequestAborted),
            inbox = await inboxStore.GetStateCountsAsync(kind, ctx.RequestAborted),
            dlxAvailable = dlx?.IsAvailable ?? false,
            pollIntervalMs = options.Value.MetricsPollIntervalMs,
            serverTimeUtc = DateTime.UtcNow
        });
    }

    private async Task HandleMetricsAsync(HttpContext ctx)
    {
        var metrics = services.GetService<IMessagingDashboardMetricsProvider>();
        if (metrics == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(ctx, new { available = false });
            return;
        }

        await WriteJsonAsync(ctx, metrics.GetSnapshot());
    }

    private async Task HandleSubscriptionsAsync(HttpContext ctx)
    {
        var providers = services.GetServices<IMessagingDashboardSubscriptionsProvider>().ToList();
        if (providers.Count == 0)
        {
            await WriteJsonAsync(ctx, new { available = false, items = Array.Empty<MessagingSubscriptionDto>() });
            return;
        }

        var items = providers
            .SelectMany(p => p.GetSubscriptions())
            .OrderBy(x => x.TypeName, StringComparer.Ordinal)
            .ThenBy(x => x.RoutingKey, StringComparer.Ordinal)
            .ToList();

        await WriteJsonAsync(ctx, new { available = true, items });
    }

    private async Task HandleOutboxQueryAsync(HttpContext ctx)
    {
        var q = ctx.Request.Query;
        var request = new MessageOutboxQueryRequest
        {
            State = ParseOutboxState(q["state"]),
            Kind = ParseKind(q["kind"]),
            Destination = q["destination"],
            TypeName = FirstNonEmpty(q["typeName"], q["eventType"]),
            Keyword = q["keyword"],
            Content = q["content"],
            MessageId = ParseGuid(FirstNonEmpty(q["messageId"], q["eventId"])),
            TransactionId = q["transactionId"],
            From = ParseDateTime(q["from"]),
            To = ParseDateTime(q["to"]),
            MinTimesSent = ParseNullableInt(q["minTimesSent"]),
            MaxTimesSent = ParseNullableInt(q["maxTimesSent"]),
            PageIndex = ParseInt(q["pageIndex"], 1),
            PageSize = ParseInt(q["pageSize"], 20)
        };
        await WriteJsonAsync(ctx, await outboxStore.QueryAsync(request, ctx.RequestAborted));
    }

    private async Task HandleOutboxFindAsync(HttpContext ctx, Guid messageId)
    {
        var item = await outboxStore.FindAsync(messageId, ctx.RequestAborted);
        if (item == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteJsonAsync(ctx, item);
    }

    private async Task HandleOutboxRetryAsync(HttpContext ctx, Guid messageId)
    {
        var body = await ReadJsonAsync<MessageOutboxRetryRequest>(ctx) ?? new MessageOutboxRetryRequest();
        // 兼容旧字段 publishNow
        if (ctx.Request.ContentLength is > 0)
        {
            ctx.Request.Body.Position = 0;
        }

        try
        {
            await outboxStore.ResetForRetryAsync(messageId, ctx.RequestAborted);

            var inboxCleared = false;
            if (body.ClearInbox)
                inboxCleared = await inboxStore.TryResetForRetryAsync(messageId, cancellationToken: ctx.RequestAborted);

            MessageDispatchOneResult? dispatchResult = null;
            if (body.DispatchNow)
            {
                try
                {
                    var ok = await outboxDispatcher.DispatchOneAsync(messageId, ctx.RequestAborted);
                    var current = await outboxStore.FindAsync(messageId, ctx.RequestAborted);
                    dispatchResult = new MessageDispatchOneResult(
                        ok,
                        ok ? null : "Message was skipped (already in progress or not in a publishable state).",
                        current?.State);
                }
                catch (Exception ex)
                {
                    dispatchResult = new MessageDispatchOneResult(false, ex.Message, MessageOutboxState.PublishFailed);
                }
            }

            await WriteJsonAsync(ctx, new
            {
                messageId,
                reset = true,
                inboxCleared,
                dispatchNow = body.DispatchNow,
                published = dispatchResult?.Dispatched ?? false,
                dispatched = dispatchResult?.Dispatched ?? false,
                error = dispatchResult?.Error,
                state = dispatchResult?.State
            });
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(ctx, new { messageId, error = ex.Message, published = false, dispatched = false });
        }
    }

    private async Task HandleInboxQueryAsync(HttpContext ctx)
    {
        var q = ctx.Request.Query;
        var request = new MessageInboxQueryRequest
        {
            State = ParseInboxState(q["state"]),
            Kind = ParseKind(q["kind"]),
            ConsumerGroup = q["consumerGroup"],
            Destination = q["destination"],
            TypeName = FirstNonEmpty(q["typeName"], q["eventType"]),
            Keyword = q["keyword"],
            Content = q["content"],
            MessageId = ParseGuid(FirstNonEmpty(q["messageId"], q["eventId"])),
            Error = q["error"],
            From = ParseDateTime(q["from"]),
            To = ParseDateTime(q["to"]),
            PageIndex = ParseInt(q["pageIndex"], 1),
            PageSize = ParseInt(q["pageSize"], 20)
        };
        await WriteJsonAsync(ctx, await inboxStore.QueryAsync(request, ctx.RequestAborted));
    }

    private async Task HandleInboxFindAsync(HttpContext ctx, Guid messageId)
    {
        var consumerGroup = ctx.Request.Query["consumerGroup"].FirstOrDefault();
        var item = await inboxStore.FindAsync(messageId, consumerGroup, ctx.RequestAborted);
        if (item == null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteJsonAsync(ctx, item);
    }

    private async Task HandleInboxRetryAsync(HttpContext ctx, Guid messageId)
    {
        var body = await ReadJsonAsync<MessageInboxRetryRequest>(ctx) ?? new MessageInboxRetryRequest();
        var consumerGroup = ctx.Request.Query["consumerGroup"].FirstOrDefault();
        try
        {
            await inboxStore.ResetForRetryAsync(messageId, consumerGroup, ctx.RequestAborted);

            MessageInboxReprocessResult? reprocess = null;
            var reprocessor = services.GetService<IMessagingInboxLocalReprocessor>();
            if (body.ReprocessNow && reprocessor != null)
                reprocess = await reprocessor.ReprocessAsync(messageId, ctx.RequestAborted);

            await WriteJsonAsync(ctx, new
            {
                messageId,
                reset = true,
                reprocessNow = body.ReprocessNow,
                reprocessAvailable = reprocessor != null,
                processed = reprocess?.Processed ?? false,
                error = reprocess?.Error,
                state = reprocess?.State
            });
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(ctx, new { messageId, error = ex.Message, processed = false });
        }
    }

    private async Task HandleDlxListAsync(HttpContext ctx)
    {
        var dlx = services.GetService<IMessagingDlxAdminService>();
        if (dlx == null)
        {
            await WriteJsonAsync(ctx, new { available = false, items = Array.Empty<MessagingDlxMessageDto>() });
            return;
        }

        var maxCount = ParseInt(ctx.Request.Query["maxCount"], 50);
        await WriteJsonAsync(ctx, new
        {
            available = dlx.IsAvailable,
            items = await dlx.GetMessagesAsync(maxCount, ctx.RequestAborted)
        });
    }

    private async Task HandleDlxRequeueAsync(HttpContext ctx, string messageId)
    {
        var dlx = services.GetService<IMessagingDlxAdminService>()
                  ?? throw new InvalidOperationException("DLX admin is not available.");
        await dlx.RequeueAsync(messageId, ctx.RequestAborted);
        await WriteJsonAsync(ctx, new { messageId, requeued = true });
    }

    private async Task HandleDlxDiscardAsync(HttpContext ctx, string messageId)
    {
        var dlx = services.GetService<IMessagingDlxAdminService>()
                  ?? throw new InvalidOperationException("DLX admin is not available.");
        await dlx.DiscardAsync(messageId, ctx.RequestAborted);
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

    private static MessageOutboxState? ParseOutboxState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MessageOutboxState>(value, ignoreCase: true, out var state) ? state : null;
    }

    private static MessageInboxState? ParseInboxState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MessageInboxState>(value, ignoreCase: true, out var state) ? state : null;
    }

    private static MessageKind? ParseKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MessageKind>(value, ignoreCase: true, out var kind) ? kind : null;
    }

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(value, out var n) ? n : fallback;

    private static int? ParseNullableInt(string? value)
        => int.TryParse(value, out var n) ? n : null;

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var id) ? id : null;

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out var dt))
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            };
        }

        return null;
    }
}
