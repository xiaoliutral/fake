using Fake.Data.Seeding;
using Fake.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.IntegrationEventLog.DataSeeds;

public class IntegrationEventLogDataSeedContributor(
    IntegrationEventLogContext dbContext,
    ILogger<IntegrationEventLogDataSeedContributor> logger)
    : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync()
    {
        try
        {
            logger.LogInformation("开始初始化 IntegrationEventLog 数据库...");

            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("发现 {Count} 个待应用的迁移", pending.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("IntegrationEventLog 迁移应用成功");
            }
            else
            {
                logger.LogInformation("IntegrationEventLog 没有待应用的迁移");
            }

            logger.LogInformation("IntegrationEventLog 数据库初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IntegrationEventLog 数据库初始化失败");
            throw;
        }
    }
}
