using Fake;
using Fake.Modularity;
using Fake.Testing;

public abstract class ApplicationTestBase<TStartupModule> : ApplicationTestWithTools<TStartupModule>
    where TStartupModule : IFakeModule
{
    protected override void SetApplicationCreationOptions(FakeApplicationCreationOptions options)
    {
        options.UseAutofac();
        options.Configuration.UserSecretsId = "7b7d01c9-fd52-4fed-a6aa-782428555b40";
        options.Configuration.EnvironmentName = "Development";
    }
}