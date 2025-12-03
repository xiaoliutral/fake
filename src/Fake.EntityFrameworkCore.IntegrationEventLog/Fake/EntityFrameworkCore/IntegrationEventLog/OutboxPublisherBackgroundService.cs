using Fake.EntityFrameworkCore.IntegrationEventLog.Options;
using Fake.EventBus.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

/// <summary>
/// Outbox 后台发布服务，定期扫描未发送的集成事件并发送到事件总线
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
        
        // 验证配置
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher Background Service is starting. Scan interval: {ScanInterval}, Batch size: {BatchSize}", 
            _options.ScanInterval, _options.BatchSize);

        // 等待指定时间后开始扫描
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
        using var scope = _serviceProvider.CreateScope();
        
        var eventLogService = scope.ServiceProvider.GetRequiredService<IOutboxEventLogService>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IDistributedEventBus>();

        // 获取所有未发送的事件（TransactionId 为 default 表示没有绑定到特定事务）
        var pendingEvents = (await eventLogService.RetrieveEventLogsPendingToPublishAsync(Guid.Empty))
            .Take(_options.BatchSize)  // 限制批量大小
            .ToList();

        if (!pendingEvents.Any())
            return;

        _logger.LogDebug("Found {Count} pending events to publish", pendingEvents.Count);

        // 并发处理事件（根据批量大小）
        var tasks = pendingEvents.Select(eventLog => ProcessSingleEventAsync(
            eventLog, eventLogService, eventBus, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleEventAsync(
        OutboxEventLogEntry eventLog,
        IOutboxEventLogService eventLogService,
        IDistributedEventBus eventBus,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            _logger.LogDebug("Attempting to acquire lock for event {EventId} ({EventType})", 
                eventLog.EventId, eventLog.EventTypeShortName);

            // 尝试获取分布式锁（支持僵尸锁恢复）
            var lockTimeout = _options.EnableZombieLockRecovery ? _options.LockTimeout : (TimeSpan?)null;
            var lockAcquired = await eventLogService.TryMarkEventAsInProgressAsync(eventLog.EventId, lockTimeout);

            if (!lockAcquired)
            {
                // 锁已被其他实例抢占，跳过此事件
                _logger.LogDebug("Event {EventId} already being processed by another instance, skipping", 
                    eventLog.EventId);
                return;
            }

            if (_options.EnableZombieLockRecovery && eventLog.TimesSent > 0)
            {
                _logger.LogWarning("Recovered zombie lock for event {EventId}, attempt {TimesSent}", 
                    eventLog.EventId, eventLog.TimesSent + 1);
            }

            _logger.LogDebug("Lock acquired, publishing event {EventId} ({EventType}) from Outbox", 
                eventLog.EventId, eventLog.EventTypeShortName);

            // 发布事件
            if (eventLog.IntegrationEvent != null)
            {
                await eventBus.PublishAsync((dynamic)eventLog.IntegrationEvent, cancellationToken);
            }

            // 标记为已发布
            await eventLogService.MarkEventAsPublishedAsync(eventLog.EventId);

            _logger.LogInformation("Successfully published event {EventId} from Outbox", eventLog.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventId} from Outbox", eventLog.EventId);
            
            // 标记为发布失败
            await eventLogService.MarkEventAsFailedAsync(eventLog.EventId);
        }
    }
}
