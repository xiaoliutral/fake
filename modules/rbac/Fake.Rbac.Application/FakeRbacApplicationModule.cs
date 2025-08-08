using Fake.Modularity;

namespace Fake.Rbac.Application;

[DependsOn(typeof(FakeRbacDomainModule))]
public class FakeRbacApplicationModule : FakeModule
{
    
}