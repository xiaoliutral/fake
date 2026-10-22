using Fake.Autofac;
using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
using Fake.EventBus.Messaging.Tests.Events;
using Fake.EventBus.Messaging.Tests.Fakes;
using Fake.Messaging;
using Fake.Messaging.EntityFrameworkCore;
using Fake.Modularity;
using Fake.SyncEx;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fake.EventBus.Messaging.Tests;

[DependsOn(typeof(FakeAutofacModule))]
[DependsOn(typeof(FakeEntityFrameworkCoreModule))]
[DependsOn(typeof(FakeEventBusMessagingModule))]
public class FakeEventBusMessagingTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<FakePhysicalDistributedEventBus>();
        context.Services.AddSingleton<IPhysicalDistributedEventBus>(sp =>
            sp.GetRequiredService<FakePhysicalDistributedEventBus>());

        // Inbox 本地 Reprocess 回退路径：非 keyed 的 IEventHandler<T>
        context.Services.AddTransient<IEventHandler<TestOutboxIntegrationEvent>, TrackingInboxHandler>();

        // 推迟 Messaging 后台扫描/清理，避免干扰单测
        context.Services.Configure<MessagingOptions>(o =>
        {
            o.OutboxStartupDelay = TimeSpan.FromHours(1);
            o.OutboxScanInterval = TimeSpan.FromHours(1);
            o.InboxCleanupStartupDelay = TimeSpan.FromHours(1);
            o.InboxCleanupInterval = TimeSpan.FromHours(1);
        });

        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        context.Services.AddDbContext<MessagingDbContext>(builder =>
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
        var ctx = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        // SQLite 测试：EnsureCreated（模型建表）。官方 Migration 含 MySQL collation，不能直接 Migrate。
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
