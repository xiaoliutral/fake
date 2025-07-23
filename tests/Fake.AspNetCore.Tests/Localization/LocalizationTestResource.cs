using Fake.Localization;
using Fake.Modularity;

namespace Fake.AspNetCore.Tests.Localization;

[DependsOn(typeof(FakeLocalizationResource))]
public sealed class LocalizationTestResource
{
}