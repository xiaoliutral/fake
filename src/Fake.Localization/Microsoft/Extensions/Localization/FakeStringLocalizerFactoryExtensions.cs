using Fake.Localization;

namespace Microsoft.Extensions.Localization;

public static class FakeStringLocalizerFactoryExtensions
{
    public static IStringLocalizer? CreateDefaultOrNull(this IStringLocalizerFactory localizerFactory)
    {
        return localizerFactory.Is<IFakeStringLocalizerFactory>()?.CreateDefaultOrNull();
    }

    public static IStringLocalizer? CreateByResourceNameOrNull(this IStringLocalizerFactory localizerFactory,
        string resourceName)
    {
        return localizerFactory.Is<IFakeStringLocalizerFactory>()?.CreateByResourceNameOrNull(resourceName);
    }
}