using Fake.Data.Seeding;
using Fake.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fake.Messaging.EntityFrameworkCore.DataSeeds;

/// <summary>
/// 应用 Messaging 迁移（fk_msg_outbox / fk_msg_inbox）。
/// </summary>
public class MessagingDataSeedContributor(
    MessagingDbContext dbContext,
    ILogger<MessagingDataSeedContributor> logger)
    : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync()
    {
        try
        {
            logger.LogInformation("开始初始化 Messaging 数据库...");

            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("发现 {Count} 个待应用的迁移", pending.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Messaging 迁移应用成功");
            }
            else
            {
                logger.LogInformation("Messaging 没有待应用的迁移");
            }

            logger.LogInformation("Messaging 数据库初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Messaging 数据库初始化失败");
            throw;
        }
    }
}
