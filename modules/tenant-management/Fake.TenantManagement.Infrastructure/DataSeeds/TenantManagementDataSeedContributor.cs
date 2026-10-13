using Fake.Data.Seeding;
using Fake.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fake.TenantManagement.Infrastructure.DataSeeds;

public class TenantManagementDataSeedContributor(
    TenantManagementDbContext dbContext,
    ILogger<TenantManagementDataSeedContributor> logger)
    : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync()
    {
        try
        {
            logger.LogInformation("开始初始化租户管理数据库...");

            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("发现 {Count} 个待应用的迁移", pending.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("租户管理迁移应用成功");
            }
            else
            {
                logger.LogInformation("租户管理没有待应用的迁移");
            }

            logger.LogInformation("租户管理数据库初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "租户管理数据库初始化失败");
            throw;
        }
    }
}
