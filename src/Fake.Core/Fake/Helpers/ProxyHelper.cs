using System.Reflection;

namespace Fake.Helpers;

public static class ProxyHelper
{
    private const string CastleProxyNamespace = "Castle.Proxies";

    /// <summary>
    /// 获取对象真实类型（剥离 Castle DynamicProxy）。
    /// </summary>
    public static Type GetUnProxiedType(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var type = obj.GetType();
        if (type.Namespace != CastleProxyNamespace)
            return type;

        var target = UnProxy(obj);
        if (!ReferenceEquals(target, obj))
            return GetUnProxiedType(target);

        // 类代理通常没有 __target，真实类型在 BaseType
        var baseType = type.GetTypeInfo().BaseType;
        return baseType is not null && baseType != typeof(object) ? baseType : type;
    }

    /// <summary>
    /// 如果是代理对象则返回动态代理目标对象，否则直接返回对象，支持 Castle 动态代理。
    /// </summary>
    public static object UnProxy(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (obj.GetType().Namespace != CastleProxyNamespace)
            return obj;

        var targetField = obj.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name == "__target");

        if (targetField == null)
            return obj;

        return targetField.GetValue(obj) ?? obj;
    }
}
