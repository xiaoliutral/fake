using Fake.EntityFrameworkCore;
using Fake.FileManagement.Domain;
using Fake.FileManagement.Domain.FileAggregate;
using Fake.FileManagement.Infrastructure.Repositories;
using Fake.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.FileManagement.Infrastructure;

[DependsOn(
    typeof(FakeFileManagementDomainModule),
    typeof(FakeEntityFrameworkCoreModule)
)]
public class FakeFileManagementInfrastructureModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IStoredFileRepository, StoredFileRepository>();

        context.Services.AddDbContext<FileManagementDbContext>(options =>
        {
            var configuration = context.Services.GetConfiguration();
            var connectionString = configuration.GetConnectionString("FileManagement")
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
