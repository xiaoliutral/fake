using Fake.EntityFrameworkCore;
using Fake.Modularity;
using Fake.Rbac.Domain.MenuAggregate;
using Fake.Rbac.Domain.RoleAggregate;
using Fake.Rbac.Domain.UserAggregate;
using Fake.Rbac.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Rbac.Infrastructure;

[DependsOn(
    typeof(FakeRbacDomainModule),
    typeof(FakeEntityFrameworkCoreModule)
)]
public class FakeRbacInfrastructureModule: FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<IUserRepository, UserRepository>();
        context.Services.AddScoped<IEfCoreUserRepository, UserRepository>();
        context.Services.AddScoped<IRoleRepository, RoleRepository>();
        context.Services.AddScoped<IEfCoreRoleRepository, RoleRepository>();
        context.Services.AddScoped<IMenuRepository, MenuRepository>();
        context.Services.AddScoped<IEfCoreMenuRepository, MenuRepository>();
    }
}