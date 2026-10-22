using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.Messaging.EntityFrameworkCore;

public class OutboxDispatcherBackgroundService(
    OutboxDispatcher dispatcher,
    IOptions<MessagingOptions> options,
    ILogger<OutboxDispatcherBackgroundService> logger) : BackgroundService
{
    private readonly MessagingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _options.Validate();

        logger.LogInformation(
            "Messaging Outbox Dispatcher starting. ScanInterval={ScanInterval}, BatchSize={BatchSize}",
            _options.OutboxScanInterval, _options.OutboxBatchSize);

        await Task.Delay(_options.OutboxStartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while dispatching outbox messages.");
            }

            await Task.Delay(_options.OutboxScanInterval, stoppingToken);
        }
    }
}
