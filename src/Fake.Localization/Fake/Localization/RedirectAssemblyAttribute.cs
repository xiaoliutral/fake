
using System.Reflection;

namespace Fake.Localization;

/// <summary>
/// 资源重定向：
///     redirects所在程序集类型遇到多语言资源选定，会重定向到此资源
/// </summary>
/// <param name="redirects">需要重定向的模块</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RedirectAssemblyAttribute(params Type[] redirects): Attribute
{
    public Assembly[] Assemblies { get; } = redirects.Select(t => t.Assembly).ToArray();
}