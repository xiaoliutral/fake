namespace Fake.Rbac.Application.Dtos.Permission;

/// <summary>
/// 权限定义 DTO
/// </summary>
public class PermissionDefinitionDto
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
    /// 子权限列表
    /// </summary>
    public List<PermissionDefinitionDto> Children { get; set; } = new();
}

