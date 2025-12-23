using Fake.Data;
using Fake.Data.Seeding;
using Fake.Domain.Repositories;
using Fake.DomainDrivenDesign.Repositories.EntityFrameWorkCore;
using Fake.EntityFrameworkCore;
using Fake.Modularity;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.OrganizationAggregate;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Rbac.Infrastructure.DataSeeds;
using Fake.Rbac.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Rbac.Infrastructure;

[DependsOn(
    typeof(FakeRbacDomainModule),
    typeof(FakeEntityFrameworkCoreModule)
)]
public class FakeRbacInfrastructureModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 注册仓储
        context.Services.AddTransient<IUserRepository, UserRepository>();
        context.Services.AddTransient<IRoleRepository, RoleRepository>();
        context.Services.AddTransient<IMenuRepository, MenuRepository>();
        context.Services.AddTransient<IOrganizationRepository, OrganizationRepository>();

        // 注册数据种子
        context.Services.AddTransient<IDataSeedContributor, RbacDataSeedContributor>();

        // 配置数据库 - 使用 RBAC 的 DbContext
        context.Services.AddDbContext<FakeRbacDbContext>(options =>
        {
            var connectionString = context.Services.GetConfiguration().GetConnectionString("Rbac");
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion);
#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
        });
    }
}