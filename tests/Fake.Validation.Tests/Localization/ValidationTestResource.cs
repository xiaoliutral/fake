using Fake.Localization;
using Fake.Validation.Localization;

namespace Fake.Validation.Tests.Localization;

[InheritResource(typeof(FakeValidationResource))]
[RedirectAssembly(typeof(FakeValidationTestModule))]
public sealed class ValidationTestResource
{
    public const string ThisFieldIsRequired = "ThisFieldIsRequired";
    public const string MaxLengthErrorMessage = "MaxLengthErrorMessage";
}