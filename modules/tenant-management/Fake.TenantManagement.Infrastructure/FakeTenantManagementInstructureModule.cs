using Fake.EntityFrameworkCore;
using Fake.Modularity;
using Fake.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.TenantManagement.Infrastructure;

[DependsOn(
    typeof(FakeTenantManagementDomainModule),
    typeof(FakeEntityFrameworkCoreModule)
)]
public class FakeTenantManagementInfrastructureModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddDbContext<TenantManagementDbContext>(options =>
        {
            var configuration = context.Services.GetConfiguration();
            var connectionString = configuration.GetConnectionString("TenantManagement")
                                   ?? configuration.GetConnectionString("Rbac")
                                   ?? configuration.GetConnectionString("Default");

            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion);
#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
        });
    }
}
