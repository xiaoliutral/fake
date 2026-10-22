using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.EntityFrameworkCore;

public class MessagingInboxCleanupBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<MessagingOptions> options,
    ILogger<MessagingInboxCleanupBackgroundService> logger) : BackgroundService
{
    private readonly MessagingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _options.Validate();

        logger.LogInformation(
            "Messaging Inbox Cleanup starting. RetentionDays={RetentionDays}, Interval={Interval}",
            _options.InboxRetentionDays, _options.InboxCleanupInterval);

        await Task.Delay(_options.InboxCleanupStartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while cleaning up inbox records.");
            }

            await Task.Delay(_options.InboxCleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-_options.InboxRetentionDays);

        var deleted = await db.InboxMessages
            .Where(e => e.State == MessageInboxState.ConsumeSucceeded && e.ProcessedTime < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            logger.LogInformation("Cleaned up {DeletedCount} old inbox records", deleted);
    }
}
