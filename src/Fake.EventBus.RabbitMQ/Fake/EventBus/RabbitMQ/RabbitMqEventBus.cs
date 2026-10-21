using System.Net.Sockets;
using Fake.EventBus;
using Fake.EventBus.Distributed;
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
        ActivityContext parentContext)
    {
        using var activity = telemetry.StartActivity($"{_queueName} process", ActivityKind.Consumer, parentContext);
        RabbitMqEventBusTelemetryService.SetActivityContext(activity, routingKey, "process");

        logger.LogDebug("Processing RabbitMQ event: {RoutingKey}", routingKey);

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        if (!TryResolveEventType(routingKey, properties, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for routing key {RoutingKey}", routingKey);
            return;
        }

        activity?.SetTag("fake.event.type", eventType.FullName);

        var @event = DeserializeMessage(message, eventType);
        activity?.SetTag("messaging.message.id", @event.Id.ToString());

        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxEventLogService>();

        var isNewEvent = await inboxService.TryMarkAsProcessingAsync(
            @event.Id,
            eventType.FullName ?? routingKey,
            message);

        if (!isNewEvent)
        {
            metrics.RecordInboxDuplicate(eventType.Name);
            logger.LogInformation("Event {EventId} already processed, skipping.", @event.Id);
            return;
        }

        logger.LogDebug("Event {EventId} marked as processing", @event.Id);

        var handlers = ResolveHandlers(scope.ServiceProvider, eventType);
        if (handlers.Count == 0)
        {
            var msg = $"No IEventHandler registered for {eventType.FullName}.";
            await inboxService.MarkAsNoHandlerAsync(@event.Id, msg);
            logger.LogWarning("Event {EventId} ({EventType}) has no matching handler", @event.Id, eventType.FullName);
            return;
        }

        const int maxRetries = 3;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 消费侧默认开非事务 UoW：Handler 可查库；写库/Outbox 请自行 RequiresNew 事务性 UoW
                using (var uow = unitOfWorkManager.Begin(new UnitOfWorkAttribute { IsTransactional = false }))
                {
                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(@event);
                    }

                    await uow.CompleteAsync();
                }

                await inboxService.MarkAsSucceededAsync(@event.Id);
                metrics.RecordInboxProcessed(eventType.Name);

                logger.LogInformation("Event {EventId} processed successfully", @event.Id);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning(ex, "Event {EventId} processing failed (attempt {Attempt}/{MaxRetries})",
                    @event.Id, attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
        }

        if (lastException != null)
        {
            await inboxService.MarkAsFailedAsync(@event.Id, lastException.Message);
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
        logger.LogDebug("Creating rabbitmq eventbus consumer channel");

        var channel = rabbitMqConnectionPool.Get(_eventBusOptions.ConnectionName).CreateModel();

        var arguments = new Dictionary<string, object>();

        if (_eventBusOptions.EnableDlx)
        {
            var dlxExchangeName = RabbitMqDlxNames.Exchange(_exchangeName);
            var dlxQueueName = RabbitMqDlxNames.Queue(_queueName);
            var dlxRouteKey = RabbitMqDlxNames.RoutingKey(_queueName);

            logger.LogDebug("Binding DLX exchange: {DlxExchangeName}, queue: {DlxQueueName}, routeKey: {DlxRouteKey}",
                dlxExchangeName, dlxQueueName, dlxRouteKey);

            channel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Direct, durable: true);
            channel.QueueDeclare(dlxQueueName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(dlxQueueName, dlxExchangeName, dlxRouteKey);

            arguments.Add("x-dead-letter-exchange", dlxExchangeName);
            arguments.Add("x-dead-letter-routing-key", dlxRouteKey);

            if (_eventBusOptions.MessageTtl > 0)
            {
                arguments.Add("x-message-ttl", _eventBusOptions.MessageTtl);
            }

            if (_eventBusOptions.QueueMaxLength > 0)
            {
                arguments.Add("x-max-length", _eventBusOptions.QueueMaxLength);
            }
        }

        channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct, durable: true);
        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments);

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

    private void StartBasicConsume(IModel channel)
    {
        logger.LogDebug("Starting rabbitmq eventbus basic consume");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            var routingKey = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
            var parentContext = telemetry.ExtractTraceContext(eventArgs.BasicProperties).ActivityContext;

            try
            {
                await ProcessingEventAsync(routingKey, message, eventArgs.BasicProperties, parentContext);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Processing event: {RoutingKey} failed, message:\n {Message}", routingKey,
                    message);

                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
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
