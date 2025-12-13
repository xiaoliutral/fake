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

            // 确保数据库已创建
            var created = await dbContext.Database.EnsureCreatedAsync();

            logger.LogInformation(created ? "数据库创建成功" : "数据库已存在，通过创建");

            // 应用所有待处理的迁移
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var migrations = pendingMigrations.ToList();
            if (migrations.Any())
            {
                logger.LogInformation($"发现 {migrations.Count()} 个待应用的迁移");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("迁移应用成功");
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
