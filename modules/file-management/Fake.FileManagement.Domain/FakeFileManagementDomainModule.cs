using Fake.DomainDrivenDesign;
using Fake.Modularity;

namespace Fake.FileManagement.Domain;

[DependsOn(typeof(FakeDddDomainModule))]
public class FakeFileManagementDomainModule : FakeModule
{
}
