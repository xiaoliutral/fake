using Fake.EntityFrameworkCore.Auditing;
using Fake.EntityFrameworkCore.Interceptors;
using Fake.Modularity;
using Fake.UnitOfWork.EntityFrameWorkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


// ReSharper disable once CheckNamespace
namespace Fake.EntityFrameworkCore;

[DependsOn(typeof(FakeDddDomainModule))]
public class FakeEntityFrameworkCoreModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        // 必须在任何 DbContext OnModelCreating / Migrate 之前生效
        FakeDbProperties.Configure(configuration[FakeDbProperties.TablePrefixConfigurationKey]);

        context.Services.AddTransient(typeof(IEfDbContextProvider<>), typeof(UowEfDbContextProvider<>));
        context.Services.AddTransient<IEntityChangeHelper, EntityChangeHelper>();
        context.Services.AddSingleton<FakeDbCommandInterceptor>();
        context.Services.AddSingleton<ICommandFormatter, FakeCommandFormatter>();
    }
}