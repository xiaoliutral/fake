using Fake.EventBus.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.EventBus.IntegrationEventLog;

/// <summary>
/// Outbox 后台发布服务，定期扫描未发送的集成事件并发送到事件总线。
/// 扫描用一个 Scope；发布时每条消息独立 Scope，从而可安全并行（EF DbContext 非线程安全）。
/// </summary>
public class OutboxPublisherBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;
    private readonly OutboxPublisherOptions _options;

    public OutboxPublisherBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherBackgroundService> logger,
        IOptions<OutboxPublisherOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;

        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox Publisher starting. ScanInterval={ScanInterval}, BatchSize={BatchSize}, MaxDegreeOfParallelism={MaxDegreeOfParallelism}",
            _options.ScanInterval, _options.BatchSize, _options.MaxDegreeOfParallelism);

        await Task.Delay(_options.StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await Task.Delay(_options.ScanInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Publisher Background Service is stopping.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        List<OutboxEventLogEntry> pendingEvents;
        using (var scanScope = _serviceProvider.CreateScope())
        {
            var eventLogService = scanScope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
            pendingEvents = (await eventLogService.RetrieveEventLogsPendingToPublishAsync(
                    _options.BatchSize, cancellationToken))
                .ToList();
        }

        if (pendingEvents.Count == 0)
            return;

        _logger.LogDebug("Found {Count} pending events to publish", pendingEvents.Count);

        // 并发安全前提：每条消息独立 Scope（独立 DbContext），抢占靠 ExecuteUpdate 行锁
        await Parallel.ForEachAsync(
            pendingEvents,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (eventLog, ct) => await ProcessSingleEventAsync(eventLog, ct));
    }

    private async Task ProcessSingleEventAsync(
        OutboxEventLogEntry eventLog,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var eventLogService = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IDistributedEventBus>();

        try
        {
            _logger.LogDebug("Attempting to acquire lock for event {EventId} ({EventType})",
                eventLog.EventId, eventLog.EventTypeShortName);

            var lockTimeout = _options.EnableZombieLockRecovery ? _options.LockTimeout : (TimeSpan?)null;
            var lockAcquired = await eventLogService.TryMarkEventAsInProgressAsync(eventLog.EventId, lockTimeout);

            if (!lockAcquired)
            {
                _logger.LogDebug("Event {EventId} already being processed by another instance, skipping",
                    eventLog.EventId);
                return;
            }

            if (_options.EnableZombieLockRecovery && eventLog.TimesSent > 0)
            {
                _logger.LogWarning("Recovered zombie lock for event {EventId}, attempt {TimesSent}",
                    eventLog.EventId, eventLog.TimesSent + 1);
            }

            if (eventLog.IntegrationEvent is not IntegrationEvent integrationEvent)
            {
                throw new InvalidOperationException(
                    $"Outbox entry {eventLog.EventId} has no deserializable IntegrationEvent.");
            }

            _logger.LogDebug("Lock acquired, publishing event {EventId} ({EventType}) from Outbox",
                eventLog.EventId, eventLog.EventTypeShortName);

            await eventBus.PublishAsync(integrationEvent, cancellationToken);
            await eventLogService.MarkEventAsPublishedAsync(eventLog.EventId);

            _logger.LogInformation("Successfully published event {EventId} from Outbox", eventLog.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventId} from Outbox", eventLog.EventId);
            try
            {
                await eventLogService.MarkEventAsFailedAsync(eventLog.EventId);
            }
            catch (Exception markEx)
            {
                _logger.LogError(markEx, "Failed to mark event {EventId} as PublishFailed", eventLog.EventId);
            }
        }
    }
}
