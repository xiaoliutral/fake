using Fake.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fake.FileManagement.Infrastructure.DataSeeds;

public class FileManagementDbInitializer(
    FileManagementDbContext dbContext,
    ILogger<FileManagementDbInitializer> logger) : ITransientDependency
{
    public async Task InitializeAsync()
    {
        try
        {
            logger.LogInformation("开始初始化文件管理数据库...");

            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("发现 {Count} 个待应用的迁移", pending.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("文件管理迁移应用成功");
            }
            else
            {
                logger.LogInformation("文件管理没有待应用的迁移");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "文件管理数据库初始化失败");
            throw;
        }
    }
}
