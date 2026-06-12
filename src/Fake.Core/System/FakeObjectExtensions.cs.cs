using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Fake;

namespace System;

public static class FakeObjectExtensions
{
    #region IsIn

    /// <summary>
    /// item是否在给定list中
    /// </summary>
    /// <param name="item"></param>
    /// <param name="list"></param>
    /// <typeparam name="T"></typeparam>
    public static bool IsIn<T>(this T item, params T[] list)
    {
        return list.Contains(item);
    }

    public static bool IsIn<T>(this T item, IEnumerable<T> items)
    {
        return items.Contains(item);
    }

    #endregion

    
    /// <summary>
    /// 将obj弱转为给定类型T，等价于obj as T
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? As<T>(this object? obj) where T : class
    {
        return obj as T;
    }

    /// <summary>
    /// 将obj强转为给定类型T
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidCastException">无法将对象强转为给定类型</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Is<T>(this object obj) where T : class
    {
        return (T)obj;
    }
    
    /// <summary>
    /// 将obj强转为给定类型T
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidCastException">无法将对象强转为给定类型</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T To<T>(this object obj) where T : struct
    {
        var targetType = typeof(T);
        
        // 处理 Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var actualType = underlyingType ?? targetType;
        
        // Convert.ChangeType() 不支持把 string/object 转成 Guid
        if (actualType == typeof(Guid))
        {
            var value = Guid.Parse(obj.ToString()!);
            return (T)(object)value;
        }
    
        return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
    }
}