using Fake.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fake.Rbac.Infrastructure.DataSeeds;

/// <summary>
/// 数据库初始化器
/// </summary>
public class DbInitializer(FakeRbacDbContext dbContext, ILogger<DbInitializer> logger) : ITransientDependency
{
    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            logger.LogInformation("开始初始化数据库...");

            // 应用所有待处理的迁移（会自动创建数据库和表）
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var migrations = pendingMigrations.ToList();
            if (migrations.Any())
            {
                logger.LogInformation("发现 {Count} 个待应用的迁移", migrations.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("迁移应用成功");
            }
            else
            {
                logger.LogInformation("没有待应用的迁移");
            }

            logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }
}
