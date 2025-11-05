namespace Fake.Rbac.Application.Dtos.Permission;

/// <summary>
/// 权限分组 DTO
/// </summary>
public class PermissionGroupDto
{
    /// <summary>
    /// 分组名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限列表
    /// </summary>
    public List<PermissionDefinitionDto> Permissions { get; set; } = new();
}

