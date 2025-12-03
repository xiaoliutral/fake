using Fake.EntityFrameworkCore.IntegrationEventLog.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

/// <summary>
/// Inbox 清理后台服务，定期删除旧的已处理事件记录
/// </summary>
public class InboxCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxCleanupBackgroundService> _logger;
    private readonly InboxCleanupOptions _options;

    public InboxCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InboxCleanupBackgroundService> logger,
        IOptions<InboxCleanupOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        
        // 验证配置
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inbox Cleanup Background Service is starting. Retention: {RetentionDays} days, Interval: {Interval}", 
            _options.RetentionDays, _options.CleanupInterval);

        // 等待指定时间后开始第一次清理
        await Task.Delay(_options.StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldInboxRecordsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up inbox records.");
            }

            await Task.Delay(_options.CleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Inbox Cleanup Background Service is stopping.");
    }

    private async Task CleanupOldInboxRecordsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        _logger.LogDebug("Starting cleanup of inbox records older than {CutoffDate}", cutoffDate);

        var deletedCount = await context.InboxEventLogs
            .Where(e => e.ProcessedTime < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleaned up {DeletedCount} old inbox records", deletedCount);
        }
        else
        {
            _logger.LogDebug("No old inbox records to clean up");
        }
    }
}
