using System.Net.Sockets;
using Fake.EventBus.Distributed;
using Fake.Messaging;
using Fake.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;

namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// 基于 RabbitMQ 的物理分布式事件总线。
/// 路由：默认 ShortName，可通过 EventBus:Routing 自定义；Header 携带 EventId / EventType。
/// </summary>
public class RabbitMqEventBus(
    IRabbitMqConnectionPool rabbitMqConnectionPool,
    ILogger<RabbitMqEventBus> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<EventBusSubscriptionOptions> subscriptionOptions,
    IOptions<RabbitMqEventBusOptions> eventBusOptions,
    IApplicationInfo applicationInfo,
    IEventRoutingKeyResolver routingKeyResolver,
    RabbitMqEventBusTelemetryService telemetry,
    EventBusMetrics metrics,
    IUnitOfWorkManager unitOfWorkManager
) : IPhysicalDistributedEventBus, IDisposable, IHostedService
{
    private readonly RabbitMqEventBusOptions _eventBusOptions = eventBusOptions.Value;
    private readonly EventBusSubscriptionOptions _subscriptionOptions = subscriptionOptions.Value;

    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(eventBusOptions.Value.RetryCount);

    private IModel? _consumerChannel;

    /// <summary>
    /// IModel 非线程安全；并发消费时 Ack/Nack 必须串行化。
    /// </summary>
    private readonly object _channelSync = new();

    private readonly string _exchangeName = eventBusOptions.Value.ExchangeName;

    private readonly string _queueName = (eventBusOptions.Value.QueueName ?? applicationInfo.ApplicationName)
        .TrimEnd('.') + ".Queue";

    public Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();
        var routingKey = routingKeyResolver.Resolve(eventType);

        using var activity = telemetry.StartActivity($"{_exchangeName} publish", ActivityKind.Producer);
        RabbitMqEventBusTelemetryService.SetActivityContext(activity, routingKey, "publish");
        activity?.SetTag("messaging.message.id", @event.Id.ToString());
        activity?.SetTag("fake.event.type", eventType.FullName);

        logger.LogDebug("Creating RabbitMQ channel for publishing event: {EventId} ({RoutingKey})", @event.Id,
            routingKey);

        using var channel = rabbitMqConnectionPool.Get(_eventBusOptions.ConnectionName).CreateModel();

        var body = SerializeMessage(@event);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers[EventBusMessageHeaders.EventId] = @event.Id.ToString();
        properties.Headers[EventBusMessageHeaders.EventType] = eventType.FullName ?? eventType.Name;

        if (activity != null)
            telemetry.InjectTraceContext(properties, activity.Context);

        _pipeline.Execute(chan =>
        {
            logger.LogDebug("Publishing event to RabbitMQ: {EventId} ({RoutingKey})", @event.Id, routingKey);
            chan.BasicPublish(exchange: _exchangeName, routingKey: routingKey, mandatory: true,
                basicProperties: properties, body: body);
        }, channel);

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RebuildSubscriptionRoutingKeys();

        _ = Task.Factory.StartNew(() =>
        {
            _consumerChannel = CreateConsumerChannel();
            foreach (var routingKey in GetBindRoutingKeys())
            {
                _consumerChannel.QueueBind(
                    queue: _queueName,
                    exchange: _exchangeName,
                    routingKey: routingKey);
            }

            StartBasicConsume(_consumerChannel);
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual IntegrationEvent DeserializeMessage(string message, Type eventType)
    {
        var res = JsonSerializer.Deserialize(message, eventType, _subscriptionOptions.JsonSerializerOptions) as
            IntegrationEvent;

        return res ?? throw new InvalidOperationException("Failed to deserialize message");
    }

    protected virtual byte[] SerializeMessage(IntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(),
            _subscriptionOptions.JsonSerializerOptions);
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
    }

    #region private methods

    private void RebuildSubscriptionRoutingKeys()
    {
        // PreConfigure 以 ShortName 注册；此处按解析器补齐自定义 / FullName 路由键映射
        var snapshot = _subscriptionOptions.EventTypes.ToList();
        foreach (var (_, eventType) in snapshot)
        {
            var routingKey = routingKeyResolver.Resolve(eventType);
            _subscriptionOptions.EventTypes[routingKey] = eventType;

            // 兼容：始终保留 ShortName 映射，便于旧消息 / 旧绑定
            _subscriptionOptions.EventTypes[eventType.Name] = eventType;
        }
    }

    private IEnumerable<string> GetBindRoutingKeys()
    {
        return _subscriptionOptions.EventTypes.Values
            .Distinct()
            .Select(routingKeyResolver.Resolve)
            .Distinct(StringComparer.Ordinal);
    }

    private async Task ProcessingEventAsync(
        string routingKey,
        string message,
        IBasicProperties properties,
        ActivityContext parentContext,
        bool redelivered)
    {
        using var activity = telemetry.StartActivity($"{_queueName} process", ActivityKind.Consumer, parentContext);
        RabbitMqEventBusTelemetryService.SetActivityContext(activity, routingKey, "process");

        logger.LogDebug("Processing RabbitMQ event: {RoutingKey} (redelivered={Redelivered})",
            routingKey, redelivered);

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        if (!TryResolveEventType(routingKey, properties, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for routing key {RoutingKey}", routingKey);
            return;
        }

        activity?.SetTag("fake.event.type", eventType.FullName);

        var @event = DeserializeMessage(message, eventType);
        activity?.SetTag("messaging.message.id", @event.Id.ToString());

        var inbox = scope.ServiceProvider.GetRequiredService<IMessageInbox>();
        var handlers = ResolveHandlers(scope.ServiceProvider, eventType);
        var consumerGroup = _queueName;
        var envelope = new MessageEnvelope
        {
            MessageId = @event.Id,
            Destination = MessageDestinations.ForEventBus(routingKey),
            Kind = MessageKind.EventBus,
            Body = message,
            TypeName = eventType.FullName ?? routingKey,
            Headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MessageHeaders.EventId] = @event.Id.ToString(),
                [MessageHeaders.EventType] = eventType.FullName ?? eventType.Name,
                [MessageHeaders.Kind] = nameof(MessageKind.EventBus)
            }
        };

        // Redelivered：原消费者已丢投递，允许抢占未过期的 Consuming，避免锁窗口内 ACK 跳过导致丢消息
        var isNewEvent = await inbox.TryBeginAsync(
            @event.Id,
            consumerGroup,
            envelope,
            forceReclaim: redelivered);

        if (!isNewEvent)
        {
            metrics.RecordInboxDuplicate(eventType.Name);
            logger.LogInformation("Event {EventId} already processed, skipping.", @event.Id);
            return;
        }

        logger.LogDebug("Event {EventId} marked as processing", @event.Id);

        if (handlers.Count == 0)
        {
            var msg = $"No IEventHandler registered for {eventType.FullName}.";
            await inbox.MarkNoHandlerAsync(@event.Id, consumerGroup);
            logger.LogWarning("Event {EventId} ({EventType}) has no matching handler: {Message}",
                @event.Id, eventType.FullName, msg);
            return;
        }

        var maxRetries = Math.Max(1, _eventBusOptions.HandlerRetryCount);
        var handlerTimeout = _eventBusOptions.HandlerTimeout;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var timeoutCts = handlerTimeout > TimeSpan.Zero
                ? new CancellationTokenSource(handlerTimeout)
                : null;
            var cancellationToken = timeoutCts?.Token ?? CancellationToken.None;

            try
            {
                // 消费侧默认开非事务 UoW：Handler 可查库；写库/Outbox 请自行 RequiresNew 事务性 UoW
                using (var uow = unitOfWorkManager.Begin(new UnitOfWorkAttribute { IsTransactional = false }))
                {
                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(@event, cancellationToken);
                    }

                    await uow.CompleteAsync(cancellationToken);
                }

                await inbox.MarkSucceededAsync(@event.Id, consumerGroup);
                metrics.RecordInboxProcessed(eventType.Name);

                logger.LogInformation("Event {EventId} processed successfully", @event.Id);
                return;
            }
            catch (OperationCanceledException ex) when (timeoutCts is { IsCancellationRequested: true })
            {
                // 挂死/超时：再重试多半仍超时，直接失败进 DLX
                lastException = new TimeoutException(
                    $"Handler timed out after {handlerTimeout} for event {@event.Id} ({eventType.Name}).", ex);
                logger.LogError(lastException,
                    "Event {EventId} handler timed out after {Timeout}", @event.Id, handlerTimeout);
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning(ex, "Event {EventId} processing failed (attempt {Attempt}/{MaxRetries})",
                    @event.Id, attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
            }
        }

        if (lastException != null)
        {
            await inbox.MarkFailedAsync(@event.Id, consumerGroup, lastException.Message);
            metrics.RecordInboxFailed(eventType.Name);
        }

        logger.LogError(lastException, "Event {EventId} failed after {MaxRetries} attempts",
            @event.Id, maxRetries);

        throw lastException!;
    }

    private static List<IEventHandler> ResolveHandlers(IServiceProvider services, Type eventType)
    {
        var handlers = services.GetKeyedServices<IEventHandler>(eventType).ToList();
        if (handlers.Count > 0)
            return handlers;

        // 兼容：仅注册了 IEventHandler<T>、未走 keyed 暴露时
        var generic = typeof(IEventHandler<>).MakeGenericType(eventType);
        return services.GetServices(generic).OfType<IEventHandler>().ToList();
    }

    private bool TryResolveEventType(string routingKey, IBasicProperties properties, out Type eventType)
    {
        if (_subscriptionOptions.EventTypes.TryGetValue(routingKey, out eventType!))
            return true;

        if (properties.Headers != null &&
            properties.Headers.TryGetValue(EventBusMessageHeaders.EventType, out var headerValue))
        {
            var typeName = headerValue switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                _ => headerValue?.ToString()
            };

            if (!string.IsNullOrEmpty(typeName))
            {
                if (_subscriptionOptions.EventTypes.TryGetValue(typeName, out eventType!))
                    return true;

                var shortName = typeName.Split('.').Last();
                if (_subscriptionOptions.EventTypes.TryGetValue(shortName, out eventType!))
                    return true;
            }
        }

        eventType = null!;
        return false;
    }

    private IModel CreateConsumerChannel()
    {
        logger.LogDebug(
            "Creating rabbitmq eventbus consumer channel (queueType={QueueType})",
            _eventBusOptions.QueueType);

        var channel = rabbitMqConnectionPool.Get(_eventBusOptions.ConnectionName).CreateModel();

        var arguments = CreateQueueArguments(includeDlxRouting: true);

        if (_eventBusOptions.EnableDlx)
        {
            var dlxExchangeName = RabbitMqDlxNames.Exchange(_exchangeName);
            var dlxQueueName = RabbitMqDlxNames.Queue(_queueName);
            var dlxRouteKey = RabbitMqDlxNames.RoutingKey(_queueName);

            logger.LogDebug("Binding DLX exchange: {DlxExchangeName}, queue: {DlxQueueName}, routeKey: {DlxRouteKey}",
                dlxExchangeName, dlxQueueName, dlxRouteKey);

            channel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Direct, durable: true);
            // DLX 与主队列使用同一队列类型，避免混用 classic/quorum
            DeclareQueue(channel, dlxQueueName, CreateQueueArguments(includeDlxRouting: false));
            channel.QueueBind(dlxQueueName, dlxExchangeName, dlxRouteKey);
        }

        channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct, durable: true);
        DeclareQueue(channel, _queueName, arguments);

        channel.BasicQos(_eventBusOptions.PrefetchSize, _eventBusOptions.PrefetchCount, false);

        channel.CallbackException += (_, args) =>
        {
            logger.LogWarning(args.Exception, "Consumer channel callback exception");

            channel.Dispose();
            var newChannel = CreateConsumerChannel();
            _consumerChannel = newChannel;
            StartBasicConsume(newChannel);
        };

        return channel;
    }

    /// <summary>
    /// 声明队列：durable=true、非 exclusive（仲裁队列强制要求）。
    /// </summary>
    private void DeclareQueue(IModel channel, string queueName, IDictionary<string, object>? arguments)
    {
        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments is { Count: > 0 } ? arguments : null);
    }

    private Dictionary<string, object> CreateQueueArguments(bool includeDlxRouting)
    {
        var arguments = new Dictionary<string, object>();

        if (_eventBusOptions.QueueType == RabbitMqQueueType.Quorum)
        {
            arguments["x-queue-type"] = "quorum";
            if (_eventBusOptions.DeliveryLimit > 0)
                arguments["x-delivery-limit"] = _eventBusOptions.DeliveryLimit;
        }

        if (includeDlxRouting && _eventBusOptions.EnableDlx)
        {
            arguments["x-dead-letter-exchange"] = RabbitMqDlxNames.Exchange(_exchangeName);
            arguments["x-dead-letter-routing-key"] = RabbitMqDlxNames.RoutingKey(_queueName);

            if (_eventBusOptions.MessageTtl > 0)
                arguments["x-message-ttl"] = _eventBusOptions.MessageTtl;

            if (_eventBusOptions.QueueMaxLength > 0)
                arguments["x-max-length"] = _eventBusOptions.QueueMaxLength;
        }

        return arguments;
    }

    private void StartBasicConsume(IModel channel)
    {
        logger.LogDebug(
            "Starting rabbitmq eventbus basic consume (prefetch={PrefetchCount}, concurrent dispatch)",
            _eventBusOptions.PrefetchCount);

        var consumer = new AsyncEventingBasicConsumer(channel);
        // 不可在 Received 内 await 业务处理：AsyncEventingBasicConsumer 会等上一条完成才派发下一条，
        // PrefetchCount > 1 也只会本地囤积。拷贝 Body 后立即返回，由并发 Task 处理并 Ack。
        consumer.Received += (o, eventArgs) =>
        {
            var routingKey = eventArgs.RoutingKey;
            var deliveryTag = eventArgs.DeliveryTag;
            // 当消费者崩溃，消息因未确认（unacked）而被重新放回队列后，再次被投递给消费者时，该消息的 Redelivered 属性会被设置为 true。
            var redelivered = eventArgs.Redelivered;
            // Body 缓冲可能被回收：进并发 Task 前必须拷贝
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
            var properties = eventArgs.BasicProperties;
            var parentContext = telemetry.ExtractTraceContext(properties).ActivityContext;

            _ = DispatchConsumedAsync(
                channel, deliveryTag, routingKey, message, properties, parentContext, redelivered);
            return Task.CompletedTask;
        };

        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
    }

    private async Task DispatchConsumedAsync(
        IModel channel,
        ulong deliveryTag,
        string routingKey,
        string message,
        IBasicProperties properties,
        ActivityContext parentContext,
        bool redelivered)
    {
        try
        {
            await ProcessingEventAsync(routingKey, message, properties, parentContext, redelivered);
            Acknowledge(channel, deliveryTag, ack: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Processing event: {RoutingKey} failed, message:\n {Message}", routingKey,
                message);
            Acknowledge(channel, deliveryTag, ack: false);
        }
    }

    /// <summary>
    /// 官方约束就是：一个 channel 同一时刻只给一个线程用。
    /// 业务可以多线程，但碰到这个 channel 的调用（尤其 Ack/Nack/Publish）要串行（加锁，或每个并发消费者用自己的 channel）。
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="deliveryTag"></param>
    /// <param name="ack"></param>
    private void Acknowledge(IModel channel, ulong deliveryTag, bool ack)
    {
        lock (_channelSync)
        {
            if (!channel.IsOpen)
                return;

            if (ack)
                channel.BasicAck(deliveryTag, multiple: false);
            else // 进入DLX
                channel.BasicNack(deliveryTag, multiple: false, requeue: false);
        }
    }

    private static ResiliencePipeline CreateResiliencePipeline(int retryCount)
    {
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>(),
            MaxRetryAttempts = retryCount,
            DelayGenerator = context => ValueTask.FromResult(GenerateDelay(context.AttemptNumber))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();

        static TimeSpan? GenerateDelay(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }
    }

    #endregion
}
