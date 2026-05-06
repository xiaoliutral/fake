using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Fake.Helpers;

public static class ReflectionHelper
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertiesCaches = new();
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyList<Type>> AssemblyCaches = new();
    private static readonly ConcurrentDictionary<string, IReadOnlyList<Attribute>> AttributeCache = new();
    private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> ConstructorCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object?>> ParameterlessConstructorCache = new();
    private static readonly ConcurrentDictionary<ConstructorInfo, Func<object?[], object?>> ConstructorInvokerCache = new();
    private static readonly MethodInfo ConvertArgumentMethod = typeof(ReflectionHelper)
        .GetMethod(nameof(ConvertArgument), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// 尝试为给定对象的属性赋值
    /// </summary>
    /// <param name="obj">给定对象</param>
    /// <param name="propertySelector">属性选择器</param>
    /// <param name="valueFactory">值工厂</param>
    /// <param name="ignoreAttributeTypes">忽略特性，如果属性标记了，则忽略赋值</param>
    public static void TrySetProperty<TObject, TValue>(TObject obj, Expression<Func<TObject, TValue>> propertySelector,
        Func<TValue> valueFactory, params Type[] ignoreAttributeTypes)
    {
        TrySetProperty(obj, propertySelector, _ => valueFactory(), ignoreAttributeTypes);
    }

    /// <summary>
    /// 尝试为给定对象的属性赋值
    /// </summary>
    /// <param name="obj">给定对象</param>
    /// <param name="propertySelector">属性选择器</param>
    /// <param name="valueFactory">值工厂</param>
    /// <param name="ignoreAttributeTypes">忽略特性，如果属性标记了，则忽略赋值</param>
    public static void TrySetProperty<TObject, TValue>(TObject obj, Expression<Func<TObject, TValue>> propertySelector,
        Func<TObject, TValue> valueFactory, params Type[] ignoreAttributeTypes)
    {
        if (obj is null) return;

        var cacheKey = $"{obj.GetType().FullName}-{propertySelector}-{ignoreAttributeTypes.JoinAsString("-")}";

        var property = PropertiesCaches.GetOrAdd(cacheKey, PropertyFactory);

        property?.SetValue(obj, valueFactory(obj));
        return;

        PropertyInfo? PropertyFactory(string _)
        {
            MemberExpression? memberExpression;
            switch (propertySelector.Body.NodeType)
            {
                // importance：处理一元表达式-ex可空
                case ExpressionType.Convert:
                {
                    memberExpression = propertySelector.Body.As<UnaryExpression>()?.Operand as MemberExpression;
                    break;
                }
                case ExpressionType.MemberAccess:
                {
                    memberExpression = propertySelector.Body.As<MemberExpression>();
                    break;
                }
                default:
                {
                    return null;
                }
            }

            if (memberExpression == null)
            {
                return null;
            }

            var propertyInfo = obj.GetType()
                .GetProperties()
                .FirstOrDefault(x => x.Name == memberExpression.Member.Name);

            if (propertyInfo == null)
            {
                return null;
            }

            var propPrivateSetMethod = propertyInfo.GetSetMethod(true);
            if (propPrivateSetMethod == null)
            {
                return null;
            }

            // 如果定义了忽略特性
            if (ignoreAttributeTypes.Any(ignoreAttribute => propertyInfo.IsDefined(ignoreAttribute, true)))
            {
                return null;
            }

            return propertyInfo;
        }
    }


    /// <summary>
    /// 获取给定成员指定特性，如果没有则返回defaultValue
    /// </summary>
    /// <param name="memberInfo">给定成员</param>
    /// <param name="defaultValue">没找到的默认值</param>
    /// <param name="inherit">从该成员的继承链上找</param>
    /// <param name="includeDeclaringType">从定义类型上寻找</param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static TAttribute? GetAttributeOrDefault<TAttribute>(
        MemberInfo memberInfo,
        TAttribute? defaultValue = null,
        bool inherit = true,
        bool includeDeclaringType = true)
        where TAttribute : Attribute
    {
        return GetAttributes<TAttribute>(memberInfo, inherit, includeDeclaringType).FirstOrDefault() ?? defaultValue;
    }

    /// <summary>
    /// 获取给定成员指定特性集合
    /// </summary>
    /// <param name="memberInfo">给定成员</param>
    /// <param name="inherit">从该成员的继承链上找</param>
    /// <param name="includeDeclaringType">从定义类型上寻找</param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(MemberInfo memberInfo,
        bool inherit = true, bool includeDeclaringType = true)
        where TAttribute : Attribute
    {
        var key = $"{memberInfo}-{typeof(TAttribute)}-{inherit}-{includeDeclaringType}";

        var value = AttributeCache.GetOrAdd(key, AttributeFactory);

        return value.Cast<TAttribute>().ToImmutableArray();

        IReadOnlyList<TAttribute> AttributeFactory(string _)
        {
            return memberInfo.GetCustomAttributes<TAttribute>(inherit)
                .Concat(memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes<TAttribute>(inherit)
                        ?? []).ToImmutableArray();
        }
    }


    /// <summary>
    /// 获取类型实例
    /// </summary>
    /// <param name="type"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static object? CreateInstance(Type type, params object?[] args)
    {
        ThrowHelper.ThrowIfNull(type, nameof(type));

        // fast path
        if (args.Length == 0)
        {
            return ParameterlessConstructorCache.GetOrAdd(type, CreateParameterlessConstructorInvoker)();
        }
        
        // 缓存公共构造函数元数据和已编译的构造调用委托，减少重复反射开销
        var constructor = FindCompatibleConstructor(type, args);
        if (constructor == null)
        {
            throw new FakeException(
                $"无法为{type.AssemblyQualifiedName}找到匹配的构造函数，参数类型：{FormatArgumentTypes(args)}");
        }

        try
        {
            return ConstructorInvokerCache.GetOrAdd(constructor, CreateConstructorInvoker)(args);
        }
        catch (FakeException)
        {
            throw;
        }
        catch (TargetInvocationException ex)
        {
            throw new FakeException($"创建类型{type.AssemblyQualifiedName}实例时发生异常", ex.InnerException ?? ex);
        }
        catch (Exception ex)
        {
            throw new FakeException($"创建类型{type.AssemblyQualifiedName}实例失败", ex);
        }
    }

    /// <summary>
    /// 获取成员类型
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Type GetMemberType(this MemberInfo member)
    {
        if (member is PropertyInfo propertyInfo)
            return propertyInfo.PropertyType;
        if (member is MethodInfo methodInfo)
            return methodInfo.ReturnType;
        if (member is FieldInfo fieldInfo)
            return fieldInfo.FieldType;
        if ((object)member == null)
            throw new ArgumentNullException(nameof(member));
        throw new ArgumentOutOfRangeException(nameof(member));
    }

    public static IReadOnlyList<Type> GetAssemblyAllTypes(Assembly assembly)
    {
        return AssemblyCaches.GetOrAdd(assembly, _ => assembly.GetTypes());
    }

    /// <summary>
    /// 对有参构造先做一次匹配打分，优先选“精确类型匹配”，其次是“可赋值类型匹配”，最后才是可转换参数
    /// </summary>
    /// <param name="type"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="FakeException">找不到或出现同分歧义时抛出更明确的 FakeException</exception>
    private static ConstructorInfo? FindCompatibleConstructor(Type type, object?[] args)
    {
        ConstructorInfo? bestConstructor = null;
        var bestScore = int.MaxValue;

        foreach (var constructor in ConstructorCache.GetOrAdd(type, static x => x.GetConstructors()))
        {
            if (!TryCalculateMatchScore(constructor, args, out var score))
            {
                continue;
            }

            if (score < bestScore)
            {
                bestConstructor = constructor;
                bestScore = score;
                continue;
            }

            if (score == bestScore)
            {
                throw new FakeException(
                    $"为{type.AssemblyQualifiedName}找到了多个匹配的构造函数，参数类型：{FormatArgumentTypes(args)}");
            }
        }

        return bestConstructor;
    }

    private static bool TryCalculateMatchScore(ConstructorInfo constructor, object?[] args, out int score)
    {
        var parameters = constructor.GetParameters();
        if (parameters.Length != args.Length)
        {
            score = int.MaxValue;
            return false;
        }

        score = 0;

        for (var i = 0; i < parameters.Length; i++)
        {
            if (!TryCalculateArgumentScore(parameters[i].ParameterType, args[i], out var argumentScore))
            {
                score = int.MaxValue;
                return false;
            }

            score += argumentScore;
        }

        return true;
    }

    private static bool TryCalculateArgumentScore(Type parameterType, object? argument, out int score)
    {
        if (argument == null)
        {
            if (CanAssignNull(parameterType))
            {
                score = 2;
                return true;
            }

            score = int.MaxValue;
            return false;
        }

        var argumentType = argument.GetType();
        var targetType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

        if (targetType == argumentType)
        {
            score = 0;
            return true;
        }

        if (targetType.IsAssignableFrom(argumentType))
        {
            score = 1;
            return true;
        }

        if (CanConvertArgument(targetType, argument))
        {
            score = 3;
            return true;
        }

        score = int.MaxValue;
        return false;
    }

    /// <summary>
    /// 同时兼容值类型的默认构造和“显式公共无参构造函数”，避免把有自定义无参构造的 struct 错当成 default(T)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="FakeException"></exception>
    private static Func<object?> CreateParameterlessConstructorInvoker(Type type)
    {
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor != null)
        {
            var newExpression = Expression.New(constructor);
            var castExpression = Expression.Convert(newExpression, typeof(object));
            return Expression.Lambda<Func<object?>>(castExpression).Compile();
        }

        if (type.IsValueType)
        {
            var defaultValueExpression = Expression.Convert(Expression.Default(type), typeof(object));
            return Expression.Lambda<Func<object?>>(defaultValueExpression).Compile();
        }

        return () => throw new FakeException($"类型{type.AssemblyQualifiedName}不存在公共无参构造函数");
    }

    private static Func<object?[], object?> CreateConstructorInvoker(ConstructorInfo constructor)
    {
        var argumentsParameter = Expression.Parameter(typeof(object[]), "args");

        var constructorArguments = constructor.GetParameters()
            .Select(Expression (parameter, index) =>
                Expression.Convert(
                    Expression.Call(
                        ConvertArgumentMethod,
                        Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                        Expression.Constant(parameter.ParameterType, typeof(Type))
                    ),
                    parameter.ParameterType
                ))
            .ToArray();

        var newExpression = Expression.New(constructor, constructorArguments);
        var body = Expression.Convert(newExpression, typeof(object));

        return Expression.Lambda<Func<object?[], object?>>(body, argumentsParameter).Compile();
    }

    private static object? ConvertArgument(object? argument, Type parameterType)
    {
        if (argument == null)
        {
            if (CanAssignNull(parameterType))
            {
                return null;
            }

            throw new FakeException($"无法将null赋值给构造函数参数类型{parameterType.AssemblyQualifiedName}");
        }

        var targetType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (targetType.IsInstanceOfType(argument))
        {
            return argument;
        }

        if (targetType.IsEnum)
        {
            return argument is string enumName
                ? Enum.Parse(targetType, enumName, true)
                : Enum.ToObject(targetType, argument);
        }

        if (argument is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
        {
            return Convert.ChangeType(argument, targetType);
        }

        if (targetType.IsInstanceOfType(argument))
        {
            return argument;
        }

        throw new FakeException($"无法将参数类型{argument.GetType().AssemblyQualifiedName}转换为{parameterType.AssemblyQualifiedName}");
    }

    private static bool CanConvertArgument(Type targetType, object argument)
    {
        if (targetType.IsEnum)
        {
            if (argument is string)
            {
                return true;
            }

            return argument is IConvertible;
        }

        return argument is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType);
    }

    private static bool CanAssignNull(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private static string FormatArgumentTypes(object?[] args)
    {
        return args.Length == 0
            ? "[]"
            : $"[{string.Join(", ", args.Select(x => x?.GetType().AssemblyQualifiedName ?? "null"))}]";
    }
}
