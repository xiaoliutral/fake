using Fake.Autofac;
using Fake.Modularity;

// ReSharper disable once CheckNamespace
namespace Fake.Testing;

// Depends on autofac important !!
[DependsOn(typeof(FakeAutofacModule))]
public class FakeTestingModule : FakeModule
{
}