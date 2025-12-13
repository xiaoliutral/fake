namespace Fake.Rbac.Domain.Permissions;

/// <summary>
/// 权限定义
/// </summary>
public class PermissionDefinition
{
    /// <summary>
    /// 权限代码
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 父级权限代码
    /// </summary>
    public string? ParentCode { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public PermissionDefinition()
    {
    }

    public PermissionDefinition(string code, string name, string? parentCode = null, string? description = null)
    {
        Code = code;
        Name = name;
        ParentCode = parentCode;
        Description = description;
    }
}
