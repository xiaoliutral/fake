using Fake.Data.Seeding;
using Fake.DependencyInjection;

namespace Fake.FileManagement.Infrastructure.DataSeeds;

public class FileManagementDataSeedContributor(FileManagementDbInitializer dbInitializer)
    : IDataSeedContributor, ITransientDependency
{
    public Task SeedAsync() => dbInitializer.InitializeAsync();
}
