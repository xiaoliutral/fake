using Fake.Rbac.Application.Dtos.Common;

namespace Fake.Rbac.Application.Dtos.Role;

/// <summary>
/// 角色 DTO
/// </summary>
public class RoleDto : AuditedEntityDto<Guid>
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

