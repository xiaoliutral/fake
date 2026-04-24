using Fake.Modularity;

namespace Fake.Localization;

public sealed class LocalizationResource : LocalizationResourceBase
{
    public Type ResourceType { get; }

    public LocalizationResource(
        Type resourceType,
        string? defaultCultureName = null)
        : base(
            LocalizationResourceNameAttribute.GetName(resourceType),
            defaultCultureName)
    {
        ThrowHelper.ThrowIfNull(resourceType, nameof(resourceType));
        ResourceType = resourceType;
        AddInheritedResourceTypes();
    }

    private void AddInheritedResourceTypes()
    {
        var descriptors = ResourceType
            .GetCustomAttributes(true)
            .OfType<InheritResourceAttribute>();

        foreach (var descriptor in descriptors)
        {
            foreach (var inheritedResourceType in descriptor.ResourceTypes)
            {
                InheritedResourceNames.TryAdd(LocalizationResourceNameAttribute.GetName(inheritedResourceType));
            }
        }
    }
}