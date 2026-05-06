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
        AddResourceAssemblies();
    }

    private void AddResourceAssemblies()
    {
        var attributes = ResourceType
            .GetCustomAttributes(true)
            .OfType<RedirectAssemblyAttribute>();

        foreach (var attribute in attributes)
        {
            foreach (var inheritedResourceType in attribute.Assemblies)
            {
                RedirectResourceAssemblies.TryAdd(inheritedResourceType);
            }
        }
    }

    private void AddInheritedResourceTypes()
    {
        var attributes = ResourceType
            .GetCustomAttributes(true)
            .OfType<InheritResourceAttribute>();

        foreach (var attribute in attributes)
        {
            foreach (var inheritedResourceType in attribute.ResourceTypes)
            {
                InheritedResourceNames.TryAdd(LocalizationResourceNameAttribute.GetName(inheritedResourceType));
            }
        }
    }
}