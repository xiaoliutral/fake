using Fake.Autofac;
using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Fake.EventBus.IntegrationEventLog.Tests.Events;
using Fake.EventBus.IntegrationEventLog.Tests.Fakes;
using Fake.Modularity;
using Fake.SyncEx;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.IntegrationEventLog.Tests;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeEntityFrameworkCoreModule))]
[DependsOn(typeof(FakeEventBusIntegrationEventLogModule))]
public class FakeEventBusIntegrationEventLogTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<FakePhysicalDistributedEventBus>();
        context.Services.AddSingleton<IPhysicalDistributedEventBus>(sp =>
            sp.GetRequiredService<FakePhysicalDistributedEventBus>());

        // Inbox 本地 Reprocess 回退路径：非 keyed 的 IEventHandler<T>
        context.Services.AddTransient<IEventHandler<TestOutboxIntegrationEvent>, TrackingInboxHandler>();

        context.Services.Configure<OutboxPublisherOptions>(o =>
        {
            o.StartupDelay = TimeSpan.FromHours(1);
            o.ScanInterval = TimeSpan.FromHours(1);
        });
        context.Services.Configure<InboxCleanupOptions>(o =>
        {
            o.StartupDelay = TimeSpan.FromHours(1);
            o.CleanupInterval = TimeSpan.FromHours(1);
        });

        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        // Scoped：同一 scope 内 Outbox 与业务共享同一 DbContext 实例
        context.Services.AddDbContext<IntegrationEventLogContext>(builder =>
        {
            builder.UseSqlite(connection).UseLoggerFactory(LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder
                    .AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information)
                    .AddConsole();
            }));
#if DEBUG
            builder.EnableSensitiveDataLogging();
#endif
        }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);
    }

    public override void PreConfigureApplication(ApplicationConfigureContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IntegrationEventLogContext>();
        // SQLite 测试：EnsureCreated（模型建表）。官方 Migration 含 MySQL collation，不能直接 Migrate。
        // 写入 __EFMigrationsHistory，避免后续 DataSeed 再 Migrate 撞表。
        SyncContext.Run(async () =>
        {
            await ctx.Database.EnsureCreatedAsync();
            await ctx.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """);
            foreach (var migrationId in ctx.Database.GetMigrations())
            {
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                     INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                     VALUES ({migrationId}, {"8.0.0"});
                     """);
            }
        });
    }
}
