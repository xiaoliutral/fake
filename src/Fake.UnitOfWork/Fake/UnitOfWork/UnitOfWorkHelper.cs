using System.Reflection;

namespace Fake.UnitOfWork;

public class UnitOfWorkHelper : IUnitOfWorkHelper
{
    public static bool IsUnitOfWorkType(TypeInfo implementationType)
    {
        //类定义了UnitOfWorkAttribute
        if (implementationType.IsDefined(typeof(UnitOfWorkAttribute)))
        {
            return true;
        }

        //类中有实例方法定义了UnitOfWorkAttribute
        if (implementationType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(m => m.IsDefined(typeof(UnitOfWorkAttribute)))
           )
        {
            return true;
        }

        //实现了IUnitOfWorkEnabled
        if (implementationType.IsAssignableTo(typeof(IUnitOfWorkEnabled)))
        {
            return true;
        }

        return false;
    }

    public bool IsUnitOfWorkMethod(MethodInfo methodInfo, out UnitOfWorkAttribute? unitOfWorkAttribute)
    {
        ThrowHelper.ThrowIfNull(methodInfo, nameof(methodInfo));

        unitOfWorkAttribute = null;
        if (methodInfo.IsDefined(typeof(DisableUnitOfWorkAttribute), true)) return false;
        // 继承体系
        unitOfWorkAttribute = GetUnitOfWorkAttributeOrNull(methodInfo);
        if (unitOfWorkAttribute is not null) return true;

        // 层次体系
        return methodInfo.DeclaringType?.GetTypeInfo().IsAssignableTo<IUnitOfWorkEnabled>() ?? false;
    }

    public UnitOfWorkAttribute? GetUnitOfWorkAttributeOrNull(MethodInfo methodInfo)
    {
        // 先从方法上找
        var attr = methodInfo.GetCustomAttribute<UnitOfWorkAttribute>(true);
        // 再从类上找
        return attr ?? methodInfo.DeclaringType?.GetTypeInfo().GetCustomAttribute<UnitOfWorkAttribute>(true);
    }

    public bool IsReadOnlyUnitOfWorkMethod(MethodInfo methodInfo)
    {
        ThrowHelper.ThrowIfNull(methodInfo, nameof(methodInfo));

        if (methodInfo.IsDefined(typeof(DisableUnitOfWorkAttribute), true)) return false;
        // 继承体系
        // 先从方法上找
        var attr = methodInfo.GetCustomAttribute<ReadOnlyUnitOfWorkAttribute>(true);
        // 再从类上找
        attr ??= methodInfo.DeclaringType?.GetTypeInfo().GetCustomAttribute<ReadOnlyUnitOfWorkAttribute>(true);
        if (attr is not null) return true;

        // 层次体系
        return methodInfo.DeclaringType?.GetTypeInfo().IsAssignableTo<IReadOnlyUnitOfWorkEnabled>() ?? false;
    }
}