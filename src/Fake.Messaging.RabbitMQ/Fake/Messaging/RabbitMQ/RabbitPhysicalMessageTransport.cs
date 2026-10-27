using System.Net.Sockets;
using System.Text;
using Fake.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Fake.Messaging.RabbitMQ;

/// <summary>
/// Destination <c>rabbit:{exchange}/{routingKey}</c> → BasicPublish（通道池；支持 Topic/Confirm）。
/// 发布侧 Polly 对齐 EventBus（<see cref="RabbitMessageOptions.RetryCount"/>）。
/// </summary>
public class RabbitPhysicalMessageTransport : IPhysicalMessageTransport, IDisposable
{
    private readonly IRabbitMqChannelPool _channelPool;
    private readonly IOptionsMonitor<RabbitMessageOptions> _messageOptions;
    private readonly ILogger<RabbitPhysicalMessageTransport> _logger;
    private readonly IDisposable? _optionsChange;
    private readonly object _pipelineGate = new();
    private ResiliencePipeline _pipeline;
    private int _pipelineRetryCount;

    public RabbitPhysicalMessageTransport(
        IRabbitMqChannelPool channelPool,
        IOptionsMonitor<RabbitMessageOptions> messageOptions,
        ILogger<RabbitPhysicalMessageTransport> logger)
    {
        _channelPool = channelPool;
        _messageOptions = messageOptions;
        _logger = logger;
        _pipelineRetryCount = messageOptions.CurrentValue.RetryCount;
        _pipeline = CreateResiliencePipeline(_pipelineRetryCount);
        _optionsChange = messageOptions.OnChange(o =>
        {
            lock (_pipelineGate)
            {
                if (o.RetryCount == _pipelineRetryCount)
                    return;
                _pipelineRetryCount = o.RetryCount;
                _pipeline = CreateResiliencePipeline(_pipelineRetryCount);
            }
        });
    }

    public bool CanPublish(string destination) => MessageDestinations.IsRabbit(destination);

    public Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var (exchange, routingKey) = MessageDestinations.ParseRabbit(envelope.Destination);
        var connectionName = envelope.Headers.GetValueOrDefault("fake-connection-name");
        var exchangeType = envelope.Headers.GetValueOrDefault(MessageHeaders.ExchangeType)
                           ?? ExchangeType.Direct;
        var confirm = string.Equals(
            envelope.Headers.GetValueOrDefault(MessageHeaders.PublisherConfirm),
            "true",
            StringComparison.OrdinalIgnoreCase);

        using var accessor = _channelPool.Acquire(
            channelName: $"msg:{exchange}/{routingKey}",
            connectionName: connectionName);
        var channel = accessor.Channel;
        channel.ExchangeDeclare(exchange, exchangeType, durable: true);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.MessageId = envelope.MessageId.ToString();
        properties.Headers ??= new Dictionary<string, object>();
        foreach (var (k, v) in envelope.Headers)
            properties.Headers[k] = v;

        var body = DecodeBody(envelope.Body);

        if (confirm)
            channel.ConfirmSelect();

        _logger.LogDebug(
            "Publishing rabbit message {MessageId} to {Exchange}/{RoutingKey} (type={ExchangeType}, confirm={Confirm})",
            envelope.MessageId, exchange, routingKey, exchangeType, confirm);

        ResiliencePipeline pipeline;
        lock (_pipelineGate)
            pipeline = _pipeline;

        pipeline.Execute(chan =>
        {
            chan.BasicPublish(exchange, routingKey, mandatory: true, properties, body);
            if (confirm)
                chan.WaitForConfirmsOrDie();
        }, channel);

        return Task.CompletedTask;
    }

    public void Dispose() => _optionsChange?.Dispose();

    private static ResiliencePipeline CreateResiliencePipeline(int retryCount)
    {
        var attempts = Math.Max(0, retryCount);
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>(),
            MaxRetryAttempts = attempts,
            DelayGenerator = context => ValueTask.FromResult(GenerateDelay(context.AttemptNumber))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();

        static TimeSpan? GenerateDelay(int attempt)
            => TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }

    private static byte[] DecodeBody(string body)
    {
        try
        {
            return Convert.FromBase64String(body);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(body);
        }
    }
}
