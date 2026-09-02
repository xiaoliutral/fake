namespace Fake.MultiTenant;

/// <summary>
/// 标记类型忽略多租户隔离。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public class IgnoreMultiTenancyAttribute : Attribute
{
}
