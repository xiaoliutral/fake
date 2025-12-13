namespace Fake.Data.Seeding;

public interface IDataSeedContributor
{
    Task SeedAsync();
}