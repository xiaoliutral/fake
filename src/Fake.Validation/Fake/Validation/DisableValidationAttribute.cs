namespace Fake.Validation;

/// <summary>
/// 标记后禁用方法入参校验
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
public class DisableValidationAttribute : Attribute
{
}