namespace Fake.Helpers;

public static class TypeHelper
{
    public static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static Type GetFirstGenericArgument(Type type)
    {
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().FirstOrDefault()!;
        }

        return type;
    }

    /// <summary>
    /// 是否是基元类型（拓展版本include string、enum、datetime..）
    /// </summary>
    /// <param name="type"></param>
    /// <param name="includeNullables"></param>
    /// <param name="includeEnums"></param>
    /// <returns></returns>
    public static bool IsPrimitiveExtended(Type type, bool includeNullables = true, bool includeEnums = false)
    {
        if (IsPrimitiveExtendedInternal(type, includeEnums))
        {
            return true;
        }

        if (includeNullables && IsNullable(type) && type.GenericTypeArguments.Length != 0)
        {
            return IsPrimitiveExtendedInternal(type.GenericTypeArguments[0], includeEnums);
        }

        return false;
    }

    private static bool IsPrimitiveExtendedInternal(Type type, bool includeEnums)
    {
        // c# primitive types：https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
        if (type.IsPrimitive)
        {
            return true;
        }

        if (includeEnums && type.IsEnum)
        {
            return true;
        }

        return type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }
}