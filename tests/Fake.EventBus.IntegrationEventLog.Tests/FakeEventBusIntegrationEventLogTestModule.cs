using Fake.Autofac;
using Fake.EntityFrameworkCore;
using Fake.EventBus.Distributed;
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
        SyncContext.Run(async () => await ctx.Database.EnsureCreatedAsync());
    }
}
