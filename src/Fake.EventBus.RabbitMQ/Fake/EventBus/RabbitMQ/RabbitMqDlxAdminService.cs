using System.Collections.Concurrent;
using Fake.EventBus.Distributed;

namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// 基于 BasicGet 的 DLX 运维：列表预览、Requeue（回主交换机）、Discard。
/// </summary>
public class RabbitMqDlxAdminService : IDlxAdminService, IDisposable
{
    private readonly IRabbitMqConnectionPool _connectionPool;
    private readonly RabbitMqEventBusOptions _options;
    private readonly string _queueName;
    private readonly string _dlxQueueName;
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqDlxAdminService> _logger;
    private readonly object _sync = new();
    private IModel? _channel;
    private readonly ConcurrentDictionary<string, PendingDlxMessage> _pending = new();

    public RabbitMqDlxAdminService(
        IRabbitMqConnectionPool connectionPool,
        IOptions<RabbitMqEventBusOptions> options,
        IApplicationInfo applicationInfo,
        ILogger<RabbitMqDlxAdminService> logger)
    {
        _connectionPool = connectionPool;
        _options = options.Value;
        _logger = logger;
        _exchangeName = _options.ExchangeName;
        _queueName = (_options.QueueName ?? applicationInfo.ApplicationName).TrimEnd('.') + ".Queue";
        _dlxQueueName = RabbitMqDlxNames.Queue(_queueName);
    }

    public bool IsAvailable => _options.EnableDlx;

    public Task<IReadOnlyList<DlxMessageDto>> GetMessagesAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return Task.FromResult<IReadOnlyList<DlxMessageDto>>(Array.Empty<DlxMessageDto>());

        maxCount = Math.Clamp(maxCount, 1, 200);
        EnsureChannel();

        // 先把未处理的 pending 还回队列，避免列表重复占用
        ReleasePending(requeue: true);

        var results = new List<DlxMessageDto>();
        lock (_sync)
        {
            for (var i = 0; i < maxCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = _channel!.BasicGet(_dlxQueueName, autoAck: false);
                if (result == null)
                    break;

                var messageId = Guid.NewGuid().ToString("N");
                var body = Encoding.UTF8.GetString(result.Body.Span);
                var headers = ReadHeaders(result.BasicProperties);
                var dto = new DlxMessageDto(
                    messageId,
                    result.RoutingKey,
                    result.Exchange,
                    TryGetHeader(headers, EventBusMessageHeaders.EventId),
                    TryGetHeader(headers, EventBusMessageHeaders.EventType),
                    body,
                    headers,
                    TryGetDeathTime(result.BasicProperties),
                    TryGetDeathReason(result.BasicProperties));

                _pending[messageId] = new PendingDlxMessage(
                    result.DeliveryTag,
                    result.RoutingKey,
                    result.Exchange,
                    result.BasicProperties,
                    result.Body.ToArray());

                results.Add(dto);
            }
        }

        return Task.FromResult<IReadOnlyList<DlxMessageDto>>(results);
    }

    public Task RequeueAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (!_pending.TryRemove(messageId, out var pending))
            throw new InvalidOperationException(
                $"DLX message {messageId} is not in the current preview set. Call GetMessages first.");

        EnsureChannel();
        lock (_sync)
        {
            var originalRoutingKey = TryGetOriginalRoutingKey(pending)
                                     ?? pending.RoutingKey
                                     ?? throw new InvalidOperationException("Cannot resolve original routing key for requeue.");

            var props = _channel!.CreateBasicProperties();
            props.DeliveryMode = 2;
            props.Headers = pending.Properties.Headers != null
                ? new Dictionary<string, object>(pending.Properties.Headers)
                : new Dictionary<string, object>();

            // 清理 x-death，避免再次被误判
            props.Headers.Remove("x-death");
            props.Headers.Remove("x-first-death-exchange");
            props.Headers.Remove("x-first-death-queue");
            props.Headers.Remove("x-first-death-reason");

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: originalRoutingKey,
                mandatory: true,
                basicProperties: props,
                body: pending.Body);

            _channel.BasicAck(pending.DeliveryTag, multiple: false);
            _logger.LogInformation("DLX message {MessageId} requeued to {Exchange}/{RoutingKey}",
                messageId, _exchangeName, originalRoutingKey);
        }

        return Task.CompletedTask;
    }

    public Task DiscardAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (!_pending.TryRemove(messageId, out var pending))
            throw new InvalidOperationException(
                $"DLX message {messageId} is not in the current preview set. Call GetMessages first.");

        EnsureChannel();
        lock (_sync)
        {
            _channel!.BasicAck(pending.DeliveryTag, multiple: false);
            _logger.LogInformation("DLX message {MessageId} discarded", messageId);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            ReleasePending(requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release pending DLX messages on dispose");
        }

        lock (_sync)
        {
            _channel?.Dispose();
            _channel = null;
        }
    }

    private void EnsureChannel()
    {
        lock (_sync)
        {
            if (_channel is { IsOpen: true })
                return;

            _channel?.Dispose();
            _channel = _connectionPool.Get(_options.ConnectionName).CreateModel();
        }
    }

    private void ReleasePending(bool requeue)
    {
        if (_pending.IsEmpty || _channel is not { IsOpen: true })
        {
            _pending.Clear();
            return;
        }

        lock (_sync)
        {
            foreach (var key in _pending.Keys.ToList())
            {
                if (!_pending.TryRemove(key, out var pending))
                    continue;

                try
                {
                    if (requeue)
                        _channel.BasicNack(pending.DeliveryTag, multiple: false, requeue: true);
                    else
                        _channel.BasicAck(pending.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to release DLX delivery tag {DeliveryTag}", pending.DeliveryTag);
                }
            }
        }
    }

    private static string? TryGetOriginalRoutingKey(PendingDlxMessage pending)
    {
        if (pending.Properties.Headers == null)
            return null;

        // x-death[0].routing-keys[0] or x-first-death-queue related
        if (pending.Properties.Headers.TryGetValue("x-death", out var deathObj) &&
            deathObj is List<object> deaths &&
            deaths.Count > 0 &&
            deaths[0] is Dictionary<string, object> first)
        {
            if (first.TryGetValue("routing-keys", out var keysObj) &&
                keysObj is List<object> keys &&
                keys.Count > 0)
            {
                return keys[0]?.ToString();
            }

            if (first.TryGetValue("routing-key", out var rk))
                return rk?.ToString();
        }

        return null;
    }

    private static Dictionary<string, string> ReadHeaders(IBasicProperties properties)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (properties.Headers == null)
            return result;

        foreach (var (key, value) in properties.Headers)
        {
            result[key] = value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                null => string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        return result;
    }

    private static string? TryGetHeader(IReadOnlyDictionary<string, string> headers, string key)
        => headers.TryGetValue(key, out var v) ? v : null;

    private static DateTime? TryGetDeathTime(IBasicProperties properties)
    {
        if (properties.Headers == null ||
            !properties.Headers.TryGetValue("x-death", out var deathObj) ||
            deathObj is not List<object> { Count: > 0 } deaths ||
            deaths[0] is not Dictionary<string, object> first ||
            !first.TryGetValue("time", out var timeObj))
            return null;

        return timeObj switch
        {
            AmqpTimestamp ts => DateTimeOffset.FromUnixTimeSeconds(ts.UnixTime).UtcDateTime,
            DateTime dt => dt.ToUniversalTime(),
            _ => null
        };
    }

    private static string? TryGetDeathReason(IBasicProperties properties)
    {
        if (properties.Headers == null)
            return null;

        if (properties.Headers.TryGetValue("x-first-death-reason", out var reason))
        {
            return reason switch
            {
                byte[] b => Encoding.UTF8.GetString(b),
                string s => s,
                _ => reason?.ToString()
            };
        }

        if (properties.Headers.TryGetValue("x-death", out var deathObj) &&
            deathObj is List<object> { Count: > 0 } deaths &&
            deaths[0] is Dictionary<string, object> first &&
            first.TryGetValue("reason", out var r))
        {
            return r?.ToString();
        }

        return null;
    }

    private sealed record PendingDlxMessage(
        ulong DeliveryTag,
        string RoutingKey,
        string Exchange,
        IBasicProperties Properties,
        byte[] Body);
}
