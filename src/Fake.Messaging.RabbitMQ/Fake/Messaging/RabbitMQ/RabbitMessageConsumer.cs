using System.Collections.Concurrent;
using System.Text;
using Fake.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 独立队列可靠消费：并发 + Ack 锁 + Inbox 抢占 + HandlerTimeout。
/// </summary>
public class RabbitMessageConsumer(
    IRabbitMqConnectionPool connectionPool,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMessageConsumer> logger) : IRabbitMessageConsumer, IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<string, (IModel Channel, object Sync, RabbitConsumeOptions Options)> _subscriptions = new();
    private readonly List<RabbitConsumeOptions> _pending = new();
    private readonly object _gate = new();
    private bool _started;

    public void Subscribe(RabbitConsumeOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Queue);
        ArgumentNullException.ThrowIfNull(options.Handler);

        lock (_gate)
        {
            if (!_started)
            {
                _pending.Add(options);
                return;
            }
        }

        StartSubscription(options);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<RabbitConsumeOptions> pending;
        lock (_gate)
        {
            _started = true;
            pending = _pending.ToList();
            _pending.Clear();
        }

        foreach (var options in pending)
            StartSubscription(options);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        foreach (var sub in _subscriptions.Values)
        {
            try
            {
                sub.Channel.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }

    private void StartSubscription(RabbitConsumeOptions options)
    {
        var channel = connectionPool.Get(options.ConnectionName).CreateModel();
        var sync = new object();

        DeclareTopology(channel, options);
        channel.BasicQos(0, options.FetchCount, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (o, args) =>
        {
            var bodyCopy = args.Body.ToArray();
            var headers = ExtractHeaders(args.BasicProperties);
            var deliveryTag = args.DeliveryTag;
            var redelivered = args.Redelivered;
            var routingKey = args.RoutingKey;

            _ = DispatchAsync(channel, sync, options, bodyCopy, headers, deliveryTag, routingKey, redelivered);
            return Task.CompletedTask;
        };

        channel.BasicConsume(options.Queue, autoAck: false, consumer);
        _subscriptions[options.Queue] = (channel, sync, options);

        logger.LogInformation(
            "Rabbit message consumer started on queue {Queue} (prefetch={FetchCount})",
            options.Queue, options.FetchCount);
    }

    private async Task DispatchAsync(
        IModel channel,
        object sync,
        RabbitConsumeOptions options,
        byte[] body,
        Dictionary<string, string> headers,
        ulong deliveryTag,
        string routingKey,
        bool redelivered)
    {
        try
        {
            if (!headers.TryGetValue(MessageHeaders.MessageId, out var idText) ||
                !Guid.TryParse(idText, out var messageId))
            {
                if (!string.IsNullOrEmpty(headers.GetValueOrDefault("message-id")) &&
                    Guid.TryParse(headers["message-id"], out messageId))
                {
                    // ok
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Rabbit message on {options.Queue} missing required header '{MessageHeaders.MessageId}'.");
                }
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IMessageInbox>();

            var envelope = new MessageEnvelope
            {
                MessageId = messageId,
                Destination = string.IsNullOrEmpty(options.Exchange)
                    ? MessageDestinations.ForRabbit("", routingKey)
                    : MessageDestinations.ForRabbit(options.Exchange, routingKey),
                Kind = MessageKind.Rabbit,
                Body = Convert.ToBase64String(body),
                TypeName = headers.GetValueOrDefault(MessageHeaders.EventType),
                Headers = headers
            };

            var began = await inbox.TryBeginAsync(
                messageId, options.Queue, envelope, forceReclaim: redelivered);

            if (!began)
            {
                logger.LogInformation(
                    "Rabbit message {MessageId} already processed for queue {Queue}, acking",
                    messageId, options.Queue);
                Acknowledge(channel, sync, deliveryTag, ack: true);
                return;
            }

            var ctx = new RabbitConsumeContext
            {
                MessageId = messageId,
                Queue = options.Queue,
                RoutingKey = routingKey,
                Body = body,
                Headers = headers,
                Redelivered = redelivered
            };

            Exception? last = null;
            var retries = Math.Max(1, options.HandlerRetryCount);
            for (var attempt = 1; attempt <= retries; attempt++)
            {
                using var timeoutCts = options.HandlerTimeout > TimeSpan.Zero
                    ? new CancellationTokenSource(options.HandlerTimeout)
                    : null;
                var ct = timeoutCts?.Token ?? CancellationToken.None;

                try
                {
                    await options.Handler(ctx, ct);
                    await inbox.MarkSucceededAsync(messageId, options.Queue);
                    Acknowledge(channel, sync, deliveryTag, ack: true);
                    return;
                }
                catch (OperationCanceledException ex) when (timeoutCts is { IsCancellationRequested: true })
                {
                    last = new TimeoutException(
                        $"Handler timed out after {options.HandlerTimeout} for message {messageId}.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (attempt < retries)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), CancellationToken.None);
                }
            }

            await inbox.MarkFailedAsync(messageId, options.Queue, last?.Message ?? "handler failed");
            Acknowledge(channel, sync, deliveryTag, ack: false);
            logger.LogError(last, "Rabbit message {MessageId} failed on queue {Queue}", messageId, options.Queue);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rabbit dispatch failed on queue {Queue}", options.Queue);
            Acknowledge(channel, sync, deliveryTag, ack: false);
        }
    }

    private static void Acknowledge(IModel channel, object sync, ulong deliveryTag, bool ack)
    {
        lock (sync)
        {
            if (!channel.IsOpen)
                return;

            if (ack)
                channel.BasicAck(deliveryTag, multiple: false);
            else
                channel.BasicNack(deliveryTag, multiple: false, requeue: false);
        }
    }

    private static void DeclareTopology(IModel channel, RabbitConsumeOptions options)
    {
        var arguments = new Dictionary<string, object>();
        if (options.QueueType == RabbitMqQueueType.Quorum)
        {
            arguments["x-queue-type"] = "quorum";
            if (options.DeliveryLimit > 0)
                arguments["x-delivery-limit"] = options.DeliveryLimit;
        }

        options.Declaration?.Invoke(arguments);

        if (!string.IsNullOrWhiteSpace(options.Exchange))
        {
            channel.ExchangeDeclare(options.Exchange, ExchangeType.Direct, durable: true);
            if (options.EnableDlx)
            {
                var dlxExchange = options.Exchange + ".dlx";
                var dlxQueue = options.Queue + ".dlx";
                channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true);
                channel.QueueDeclare(dlxQueue, durable: true, exclusive: false, autoDelete: false, arguments:
                    options.QueueType == RabbitMqQueueType.Quorum
                        ? new Dictionary<string, object> { ["x-queue-type"] = "quorum" }
                        : null);
                channel.QueueBind(dlxQueue, dlxExchange, options.Queue);
                arguments["x-dead-letter-exchange"] = dlxExchange;
                arguments["x-dead-letter-routing-key"] = options.Queue;
            }
        }

        channel.QueueDeclare(
            options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments.Count > 0 ? arguments : null);

        if (!string.IsNullOrWhiteSpace(options.Exchange) && !string.IsNullOrWhiteSpace(options.RoutingKey))
            channel.QueueBind(options.Queue, options.Exchange, options.RoutingKey);
    }

    private static Dictionary<string, string> ExtractHeaders(IBasicProperties properties)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(properties.MessageId))
            result[MessageHeaders.MessageId] = properties.MessageId;

        if (properties.Headers == null)
            return result;

        foreach (var (key, value) in properties.Headers)
        {
            result[key] = value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                _ => value?.ToString() ?? string.Empty
            };
        }

        return result;
    }
}
