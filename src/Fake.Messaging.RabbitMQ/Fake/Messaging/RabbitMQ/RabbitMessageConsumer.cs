using System.Collections.Concurrent;
using System.Text;
using Fake.RabbitMQ;
using Fake.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// 独立队列可靠消费：并发 + Ack 锁 + Inbox 抢占 + HandlerTimeout + ambient UoW。
/// </summary>
public class RabbitMessageConsumer(
    IRabbitMqConnectionPool connectionPool,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMessageConsumer> logger) : IRabbitMessageConsumer, IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<string, (IModel Channel, object Sync, RabbitSubscription Subscription)> _subscriptions = new();
    private readonly List<RabbitSubscription> _pending = new();
    private readonly object _gate = new();
    private bool _started;

    public void Subscribe(RabbitSubscription subscription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscription.Queue);
        ArgumentNullException.ThrowIfNull(subscription.Handler);

        lock (_gate)
        {
            if (!_started)
            {
                _pending.Add(subscription);
                return;
            }
        }

        StartSubscription(subscription);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<RabbitSubscription> pending;
        lock (_gate)
        {
            _started = true;
            pending = _pending.ToList();
            _pending.Clear();
        }

        foreach (var subscription in pending)
            StartSubscription(subscription);

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

    private void StartSubscription(RabbitSubscription subscription)
    {
        var channel = connectionPool.Get(subscription.ConnectionName).CreateModel();
        var sync = new object();

        DeclareTopology(channel, subscription);
        channel.BasicQos(subscription.PrefetchSize, subscription.PrefetchCount, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (o, args) =>
        {
            var bodyCopy = args.Body.ToArray();
            var headers = ExtractHeaders(args.BasicProperties);
            var deliveryTag = args.DeliveryTag;
            var redelivered = args.Redelivered;
            var routingKey = args.RoutingKey;

            _ = DispatchAsync(channel, sync, subscription, bodyCopy, headers, deliveryTag, routingKey, redelivered);
            return Task.CompletedTask;
        };

        channel.BasicConsume(subscription.Queue, autoAck: false, consumer);
        _subscriptions[subscription.Queue] = (channel, sync, subscription);

        logger.LogInformation(
            "Rabbit message consumer started on queue {Queue} (prefetch={PrefetchCount})",
            subscription.Queue, subscription.PrefetchCount);
    }

    private async Task DispatchAsync(
        IModel channel,
        object sync,
        RabbitSubscription subscription,
        byte[] body,
        Dictionary<string, string> headers,
        ulong deliveryTag,
        string routingKey,
        bool redelivered)
    {
        try
        {
            if (!TryResolveMessageId(headers, out var messageId))
            {
                if (subscription.AllowMissingMessageId)
                    messageId = Guid.NewGuid();
                else
                    throw new InvalidOperationException(
                        $"Rabbit message on {subscription.Queue} missing required header '{MessageHeaders.MessageId}'.");
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var inbox = sp.GetRequiredService<IMessageInbox>();

            var envelope = new MessageEnvelope
            {
                MessageId = messageId,
                Destination = string.IsNullOrEmpty(subscription.Exchange)
                    ? MessageDestinations.ForRabbit("", routingKey)
                    : MessageDestinations.ForRabbit(subscription.Exchange, routingKey),
                Kind = MessageKind.Rabbit,
                Body = Convert.ToBase64String(body),
                TypeName = headers.GetValueOrDefault(MessageHeaders.EventType),
                Headers = headers
            };

            var began = await inbox.TryBeginAsync(
                messageId, subscription.Queue, envelope, forceReclaim: redelivered);

            if (!began)
            {
                logger.LogInformation(
                    "Rabbit message {MessageId} already processed for queue {Queue}, acking",
                    messageId, subscription.Queue);
                Acknowledge(channel, sync, deliveryTag, ack: true);
                return;
            }

            var ctx = new RabbitConsumeContext
            {
                MessageId = messageId,
                Queue = subscription.Queue,
                RoutingKey = routingKey,
                Body = body,
                Headers = headers,
                Redelivered = redelivered,
                Services = sp
            };

            Exception? last = null;
            var retries = Math.Max(1, subscription.HandlerRetryCount);

            for (var attempt = 1; attempt <= retries; attempt++)
            {
                using var timeoutCts = subscription.HandlerTimeout > TimeSpan.Zero
                    ? new CancellationTokenSource(subscription.HandlerTimeout)
                    : null;
                var ct = timeoutCts?.Token ?? CancellationToken.None;

                try
                {
                    await InvokeHandlerAsync(sp, subscription, ctx, ct);
                    await inbox.MarkSucceededAsync(messageId, subscription.Queue, ct);
                    Acknowledge(channel, sync, deliveryTag, ack: true);
                    return;
                }
                catch (OperationCanceledException ex) when (timeoutCts is { IsCancellationRequested: true })
                {
                    last = new TimeoutException(
                        $"Handler timed out after {subscription.HandlerTimeout} for message {messageId}.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (attempt < retries)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), CancellationToken.None);
                }
            }

            await inbox.MarkFailedAsync(messageId, subscription.Queue, last?.Message ?? "handler failed");
            Acknowledge(channel, sync, deliveryTag, ack: false);
            logger.LogError(last, "Rabbit message {MessageId} failed on queue {Queue}", messageId, subscription.Queue);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rabbit dispatch failed on queue {Queue}", subscription.Queue);
            Acknowledge(channel, sync, deliveryTag, ack: false);
        }
    }

    private static async Task InvokeHandlerAsync(
        IServiceProvider sp,
        RabbitSubscription subscription,
        RabbitConsumeContext ctx,
        CancellationToken ct)
    {
        var mode = subscription.UnitOfWork;
        var handler = subscription.Handler
                      ?? throw new InvalidOperationException($"RabbitSubscription.Handler is required for queue {ctx.Queue}.");

        if (mode == MessageConsumeUnitOfWorkMode.None)
        {
            await handler(ctx, ct);
            return;
        }

        var attr = subscription.UnitOfWorkAttribute ?? new UnitOfWorkAttribute
        {
            IsTransactional = mode == MessageConsumeUnitOfWorkMode.Transactional
        };

        var uowManager = sp.GetRequiredService<IUnitOfWorkManager>();
        using var uow = uowManager.Begin(attr);
        await handler(ctx, ct);
        await uow.CompleteAsync(ct);
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

    private static void DeclareTopology(IModel channel, RabbitSubscription subscription)
    {
        var arguments = new Dictionary<string, object>();
        if (subscription.QueueType == RabbitMqQueueType.Quorum)
        {
            arguments["x-queue-type"] = "quorum";
            if (subscription.DeliveryLimit > 0)
                arguments["x-delivery-limit"] = subscription.DeliveryLimit;
        }

        if (subscription.MessageTtl > 0)
            arguments["x-message-ttl"] = subscription.MessageTtl;
        if (subscription.QueueMaxLength > 0)
            arguments["x-max-length"] = subscription.QueueMaxLength;

        subscription.Declaration?.Invoke(arguments);

        var exchangeType = string.IsNullOrWhiteSpace(subscription.ExchangeType)
            ? ExchangeType.Direct
            : subscription.ExchangeType;

        if (!string.IsNullOrWhiteSpace(subscription.Exchange))
        {
            channel.ExchangeDeclare(subscription.Exchange, exchangeType, durable: true);
            if (subscription.EnableDlx)
            {
                var dlxExchange = subscription.Exchange + ".dlx";
                var dlxQueue = subscription.Queue + ".dlx";
                channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true);
                channel.QueueDeclare(dlxQueue, durable: true, exclusive: false, autoDelete: false, arguments:
                    subscription.QueueType == RabbitMqQueueType.Quorum
                        ? new Dictionary<string, object> { ["x-queue-type"] = "quorum" }
                        : null);
                channel.QueueBind(dlxQueue, dlxExchange, subscription.Queue);
                arguments["x-dead-letter-exchange"] = dlxExchange;
                arguments["x-dead-letter-routing-key"] = subscription.Queue;
            }
        }

        channel.QueueDeclare(
            subscription.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments.Count > 0 ? arguments : null);

        if (!string.IsNullOrWhiteSpace(subscription.Exchange) && !string.IsNullOrWhiteSpace(subscription.RoutingKey))
            channel.QueueBind(subscription.Queue, subscription.Exchange, subscription.RoutingKey);
    }

    private static bool TryResolveMessageId(IReadOnlyDictionary<string, string> headers, out Guid messageId)
    {
        if (headers.TryGetValue(MessageHeaders.MessageId, out var idText) &&
            Guid.TryParse(idText, out messageId))
            return true;

        if (headers.TryGetValue("message-id", out var alt) && Guid.TryParse(alt, out messageId))
            return true;

        messageId = default;
        return false;
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
