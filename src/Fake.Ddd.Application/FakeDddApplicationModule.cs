using Fake.DomainDrivenDesign;
using Fake.Modularity;
using Fake.Validation;

// ReSharper disable once CheckNamespace
namespace Fake.Ddd.Application;

[DependsOn(typeof(FakeValidationModule),
    typeof(FakeDddDomainModule))]
public class FakeDddApplicationModule: FakeModule
{
}