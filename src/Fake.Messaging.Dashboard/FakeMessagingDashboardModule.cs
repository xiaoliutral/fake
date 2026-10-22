using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Messaging.Dashboard;

/// <summary>
/// Messaging Dashboard 模块。也可不依赖 Fake 模块系统，直接：
/// <code>
/// services.AddFakeMessagingDashboard(o => o.PathMatch = "/messaging");
/// app.UseFakeMessagingDashboard();
/// </code>
/// </summary>
[DependsOn(typeof(FakeMessagingModule))]
public class FakeMessagingDashboardModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<MessagingDashboardOptions>(
            configuration.GetSection(MessagingDashboardOptions.SectionName));
        context.Services.AddFakeMessagingDashboard();
    }
}
