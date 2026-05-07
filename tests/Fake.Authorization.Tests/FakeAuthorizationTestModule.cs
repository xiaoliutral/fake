using Fake.Modularity;
using Fake.Testing;

namespace Fake.Authorization.Tests;

[DependsOn(typeof(FakeAuthorizationModule))]
[DependsOn(typeof(FakeTestingModule))]
public class FakeAuthorizationTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}