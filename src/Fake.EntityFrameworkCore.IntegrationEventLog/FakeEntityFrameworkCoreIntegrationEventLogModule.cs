using Fake.EntityFrameworkCore.IntegrationEventLog.Options;
using Fake.EventBus;
using Fake.EventBus.Distributed;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.EntityFrameworkCore.IntegrationEventLog;

[DependsOn(typeof(FakeEntityFrameworkCoreModule))]
public class FakeEntityFrameworkCoreIntegrationEventLogModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 配置选项（可通过 appsettings.json 覆盖）
        context.Services.AddOptions<OutboxPublisherOptions>()
            .BindConfiguration(OutboxPublisherOptions.SectionName)
            .ValidateOnStart();
            
        context.Services.AddOptions<InboxCleanupOptions>()
            .BindConfiguration(InboxCleanupOptions.SectionName)
            .ValidateOnStart();
        
        // Outbox 模式服务
        context.Services.AddTransient<IOutboxEventLogService, OutboxEventLogService>();
        
        // Inbox 模式服务（消费端幂等性）
        context.Services.AddTransient<IInboxEventLogService, InboxEventLogService>();
        
        // Outbox 后台发布服务（支持多实例部署，自动分布式锁）
        context.Services.AddHostedService<OutboxPublisherBackgroundService>();
        
        // Inbox 清理后台服务（定期清理旧记录）
        context.Services.AddHostedService<InboxCleanupBackgroundService>();
    }
}