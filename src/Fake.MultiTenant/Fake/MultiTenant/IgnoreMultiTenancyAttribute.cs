namespace Fake.MultiTenant;

/// <summary>
/// 标记类型忽略多租户隔离（例如缓存键不附加租户前缀）
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public class IgnoreMultiTenancyAttribute : Attribute
{
}
