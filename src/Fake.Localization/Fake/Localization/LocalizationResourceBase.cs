using System.Reflection;
using Fake.Localization.Contributors;
using Microsoft.Extensions.Localization;

namespace Fake.Localization;

public abstract class LocalizationResourceBase
{
    public string ResourceName { get; }

    /// <summary>
    /// 继承的资源
    /// </summary>
    public List<string> InheritedResourceNames { get; } = [];

    public string? DefaultCultureName { get; set; }
    public List<ILocalizationResourceContributor> Contributors { get; } = [];

    /// <summary>
    /// 指定程序集专用资源，当改变IStringLocalizerFactory.Create默认行为：
    ///     当Create(Type resourceSource);资源类型不存在时，根据类型所在程序集来此集合查询，这是一个退让策略
    ///     将来，此程序集的任何类型都能找到对应的固定多语言资源
    /// </summary>
    public List<Assembly> RedirectResourceAssemblies { get; } = [];

    public LocalizationResourceBase(
        string resourceName,
        string? defaultCultureName = null)
    {
        ThrowHelper.ThrowIfNull(resourceName, nameof(resourceName));
        ResourceName = resourceName;
        DefaultCultureName = defaultCultureName;
    }

    public void Fill(
        string cultureName,
        Dictionary<string, LocalizedString> dictionary)
    {
        foreach (var contributor in Contributors)
        {
            contributor.Fill(cultureName, dictionary);
        }
    }

    public LocalizedString? GetOrNull(string cultureName, string name)
    {
        // 后者优先
        foreach (var contributor in Contributors.Select(x => x).Reverse())
        {
            var localizedString = contributor.GetOrNull(cultureName, name);

            if (localizedString != null) return localizedString;
        }

        return null;
    }
}