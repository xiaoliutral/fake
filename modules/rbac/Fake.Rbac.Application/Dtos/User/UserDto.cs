using Fake.Rbac.Application.Dtos.Common;
using Fake.Rbac.Application.Dtos.Role;

namespace Fake.Rbac.Application.Dtos.User;

/// <summary>
/// 用户 DTO
/// </summary>
public class UserDto : AuditedEntityDto<Guid>
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; set; } = string.Empty;
    
    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }
    
    /// <summary>
    /// 所属组织ID
    /// </summary>
    public Guid? OrganizationId { get; set; }
    
    /// <summary>
    /// 所属组织名称
    /// </summary>
    public string? OrganizationName { get; set; }
    
    /// <summary>
    /// 角色列表
    /// </summary>
    public List<RoleSimpleDto> Roles { get; set; } = new();
}

